using System.Collections.Concurrent;
using System.Reflection;
using GitForest.Mediator;

namespace Microsoft.Extensions.DependencyInjection;

public sealed class MediatorServiceConfiguration
{
    private readonly HashSet<Assembly> _assemblies = new();

    internal IEnumerable<Assembly> Assemblies => _assemblies;

    public void RegisterServicesFromAssembly(Assembly assembly)
    {
        if (assembly is null)
            throw new ArgumentNullException(nameof(assembly));
        _assemblies.Add(assembly);
    }
}

public static class MediatorServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(
        this IServiceCollection services,
        Action<MediatorServiceConfiguration> configure
    )
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));
        if (configure is null)
            throw new ArgumentNullException(nameof(configure));

        var cfg = new MediatorServiceConfiguration();
        configure(cfg);

        services.AddSingleton<IMediator, ServiceProviderMediator>();

        foreach (var assembly in cfg.Assemblies)
        {
            RegisterHandlersFromAssembly(services, assembly);
        }

        return services;
    }

    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly)
    {
        foreach (var type in GetLoadableTypes(assembly))
        {
            if (type is null)
                continue;
            if (type.IsAbstract || type.IsInterface)
                continue;

            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType)
                    continue;
                if (iface.GetGenericTypeDefinition() != typeof(IRequestHandler<,>))
                    continue;

                services.AddTransient(iface, type);
            }
        }
    }

    private static IEnumerable<Type?> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types;
        }
    }

    private sealed class ServiceProviderMediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;

        private static readonly ConcurrentDictionary<
            (Type RequestType, Type ResponseType),
            Func<IServiceProvider, object, CancellationToken, Task<object?>>
        > _pipelines = new();

        public ServiceProviderMediator(IServiceProvider serviceProvider)
        {
            _serviceProvider =
                serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default
        )
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            var requestType = request.GetType();
            var responseType = typeof(TResponse);

            var pipeline = _pipelines.GetOrAdd(
                (requestType, responseType),
                static key => CreatePipeline(key.RequestType, key.ResponseType)
            );
            var result = await pipeline(_serviceProvider, request, cancellationToken)
                .ConfigureAwait(false);
            return (TResponse)result!;
        }

        private static Func<
            IServiceProvider,
            object,
            CancellationToken,
            Task<object?>
        > CreatePipeline(Type requestType, Type responseType)
        {
            var method = typeof(ServiceProviderMediator).GetMethod(
                nameof(SendCore),
                BindingFlags.NonPublic | BindingFlags.Static
            );
            if (method is null)
                throw new InvalidOperationException($"Missing method {nameof(SendCore)}.");

            var closed = method.MakeGenericMethod(requestType, responseType);
            return (Func<IServiceProvider, object, CancellationToken, Task<object?>>)
                closed.CreateDelegate(
                    typeof(Func<IServiceProvider, object, CancellationToken, Task<object?>>)
                );
        }

        private static async Task<object?> SendCore<TRequest, TResponse>(
            IServiceProvider serviceProvider,
            object request,
            CancellationToken cancellationToken
        )
            where TRequest : IRequest<TResponse>
        {
            var handler = serviceProvider.GetRequiredService<
                IRequestHandler<TRequest, TResponse>
            >();
            var result = await handler
                .Handle((TRequest)request, cancellationToken)
                .ConfigureAwait(false);
            return result;
        }
    }
}
