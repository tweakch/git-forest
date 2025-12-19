using System.Net.Sockets;
using GitForest.Mediator;

namespace GitForest.Cli.Features.Connection;

public sealed record GetForestConnectionStatusQuery() : IRequest<ForestConnectionStatusResult>;

public sealed record ForestConnectionStatusResult(
    string Type,
    bool Available,
    string? Details,
    string? Error
);

internal sealed class GetForestConnectionStatusHandler
    : IRequestHandler<GetForestConnectionStatusQuery, ForestConnectionStatusResult>
{
    private readonly ForestConfig _config;

    public GetForestConnectionStatusHandler(ForestConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task<ForestConnectionStatusResult> Handle(
        GetForestConnectionStatusQuery request,
        CancellationToken cancellationToken
    )
    {
        _ = request;

        var type = string.IsNullOrWhiteSpace(_config.PersistenceProvider)
            ? ForestConfigReader.DefaultPersistenceProvider
            : _config.PersistenceProvider.Trim().ToLowerInvariant();

        if (type is not "orleans" and not "file" and not "memory")
        {
            type = ForestConfigReader.DefaultPersistenceProvider;
        }

        if (type != "orleans")
        {
            return new ForestConnectionStatusResult(
                Type: type,
                Available: true,
                Details: null,
                Error: null
            );
        }

        var orleans = _config.Orleans;
        var gatewayHost = string.IsNullOrWhiteSpace(orleans.GatewayHost)
            ? ForestConfigReader.DefaultOrleansGatewayHost
            : orleans.GatewayHost.Trim();
        var gatewayPort = orleans.GatewayPort <= 0
            ? ForestConfigReader.DefaultOrleansGatewayPort
            : orleans.GatewayPort;
        var clusterId = string.IsNullOrWhiteSpace(orleans.ClusterId)
            ? ForestConfigReader.DefaultOrleansClusterId
            : orleans.ClusterId.Trim();
        var serviceId = string.IsNullOrWhiteSpace(orleans.ServiceId)
            ? ForestConfigReader.DefaultOrleansServiceId
            : orleans.ServiceId.Trim();

        var details = $"{gatewayHost}:{gatewayPort} (clusterId={clusterId}, serviceId={serviceId})";

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(gatewayHost, gatewayPort, cancellationToken);
            return new ForestConnectionStatusResult(
                Type: type,
                Available: true,
                Details: details,
                Error: null
            );
        }
        catch (Exception ex)
        {
            return new ForestConnectionStatusResult(
                Type: type,
                Available: false,
                Details: details,
                Error: ex.Message
            );
        }
    }
}
