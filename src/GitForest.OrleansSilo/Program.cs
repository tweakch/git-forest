using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Serialization;

var builder = Host.CreateDefaultBuilder(args);

builder.UseOrleans(
    (_, silo) =>
    {
        silo.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "gitforest";
            options.ServiceId = "gitforest";
        });

        silo.UseLocalhostClustering(siloPort: 11111, gatewayPort: 30000);
        silo.AddMemoryGrainStorage("Default");
    }
);

builder.ConfigureServices(services =>
{
    services.AddSerializer(serializer => serializer.AddJsonSerializer(isSupported: _ => true));
});

await builder.Build().RunAsync();

