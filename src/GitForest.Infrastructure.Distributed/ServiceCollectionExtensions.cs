using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GitForest.Infrastructure.Distributed;

/// <summary>
/// Extension methods for registering distributed infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Orleans distributed infrastructure to the service collection.
    /// </summary>
    public static IServiceCollection AddOrleansDistributedInfrastructure(
        this IServiceCollection services,
        Action<OrleansConfiguration>? configure = null)
    {
        var config = new OrleansConfiguration();
        configure?.Invoke(config);

        services.AddOrleans(siloBuilder =>
        {
            siloBuilder.UseLocalhostClustering();

            // Configure clustering if specified
            if (config.UseAzureStorage)
            {
                // Azure Table Storage clustering (commented out as it requires connection string)
                // siloBuilder.UseAzureStorageClustering(options =>
                // {
                //     options.ConfigureTableServiceClient(config.AzureStorageConnectionString);
                // });
            }

            // Configure grain storage
            if (config.UseMemoryStorage)
            {
                siloBuilder.AddMemoryGrainStorage("grainStorage");
            }

            // Application parts are automatically discovered in Orleans 9.0+
            // No need to explicitly configure them
        });

        return services;
    }

    /// <summary>
    /// Adds Orleans client for connecting to a distributed cluster.
    /// </summary>
    public static IServiceCollection AddOrleansClient(
        this IServiceCollection services,
        Action<OrleansConfiguration>? configure = null)
    {
        var config = new OrleansConfiguration();
        configure?.Invoke(config);

        services.AddOrleansClient(clientBuilder =>
        {
            clientBuilder.UseLocalhostClustering();
        });

        return services;
    }
}

/// <summary>
/// Configuration options for Orleans distributed infrastructure.
/// </summary>
public class OrleansConfiguration
{
    public bool UseMemoryStorage { get; set; } = true;
    public bool UseAzureStorage { get; set; } = false;
    public string? AzureStorageConnectionString { get; set; }
    public int SiloPort { get; set; } = 11111;
    public int GatewayPort { get; set; } = 30000;
}
