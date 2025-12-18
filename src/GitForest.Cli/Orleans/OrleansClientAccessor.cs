using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Serialization;

namespace GitForest.Cli.Orleans;

internal sealed class OrleansClientAccessor : IAsyncDisposable
{
    private readonly ForestConfig _config;

    private readonly object _gate = new();
    private IHost? _host;
    private IClusterClient? _client;
    private Task? _startTask;

    public OrleansClientAccessor(ForestConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public IGrainFactory GrainFactory => GetOrCreateClient();

    public async Task EnsureConnectedAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default
    )
    {
        _ = GetOrCreateClient();

        Task startTask;
        lock (_gate)
        {
            _startTask ??= _host!.StartAsync(cancellationToken);
            startTask = _startTask;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        await startTask.WaitAsync(cts.Token);
    }

    private IClusterClient GetOrCreateClient()
    {
        lock (_gate)
        {
            if (_client is not null)
            {
                return _client;
            }

            var orleans = _config.Orleans;
            var clusterId = string.IsNullOrWhiteSpace(orleans.ClusterId)
                ? ForestConfigReader.DefaultOrleansClusterId
                : orleans.ClusterId.Trim();
            var serviceId = string.IsNullOrWhiteSpace(orleans.ServiceId)
                ? ForestConfigReader.DefaultOrleansServiceId
                : orleans.ServiceId.Trim();
            var gatewayHost = string.IsNullOrWhiteSpace(orleans.GatewayHost)
                ? ForestConfigReader.DefaultOrleansGatewayHost
                : orleans.GatewayHost.Trim();
            var gatewayPort = orleans.GatewayPort <= 0
                ? ForestConfigReader.DefaultOrleansGatewayPort
                : orleans.GatewayPort;

            if (!string.Equals(gatewayHost, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                // Keep first iteration simple and stable; extend later as needed.
                throw new NotSupportedException(
                    $"Only gatewayHost=localhost is supported for now (got '{gatewayHost}')."
                );
            }

            _host = Host.CreateDefaultBuilder()
                .UseOrleansClient(client =>
                {
                    client.Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = clusterId;
                        options.ServiceId = serviceId;
                    });

                    client.UseLocalhostClustering(gatewayPort: gatewayPort);

                    client.Services.AddSerializer(serializer =>
                    {
                        serializer.AddJsonSerializer(isSupported: _ => true);
                    });
                })
                .Build();

            _client = _host.Services.GetRequiredService<IClusterClient>();

            return _client;
        }
    }

    public async ValueTask DisposeAsync()
    {
        IHost? host;
        lock (_gate)
        {
            host = _host;
            _host = null;
            _client = null;
        }

        if (host is not null)
        {
            try
            {
                await host.StopAsync(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // best-effort shutdown
            }

            host.Dispose();
        }
    }
}

