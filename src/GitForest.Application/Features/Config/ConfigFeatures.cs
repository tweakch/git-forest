using GitForest.Application.Configuration;
using GitForest.Mediator;

namespace GitForest.Application.Features.Config;

public sealed record ShowConfigQuery(string ForestDir, bool Effective) : IRequest<ShowConfigResult>;

public sealed record ShowConfigResult(ForestConfig Config);

internal sealed class ShowConfigHandler : IRequestHandler<ShowConfigQuery, ShowConfigResult>
{
    public Task<ShowConfigResult> Handle(
        ShowConfigQuery request,
        CancellationToken cancellationToken
    )
    {
        _ = cancellationToken;

        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var dir = string.IsNullOrWhiteSpace(request.ForestDir)
            ? ".git-forest"
            : request.ForestDir.Trim();
        var forestDir = Path.IsPathRooted(dir)
            ? dir
            : Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, dir));

        ForestConfig config;
        if (request.Effective)
        {
            config = ForestConfigReader.ReadEffective(forestDir);
        }
        else
        {
            config =
                ForestConfigReader.TryRead(forestDir)
                ?? new ForestConfig(
                    PersistenceProvider: string.Empty,
                    LocksTimeoutSeconds: 0,
                    Reconcile: new ReconcileConfig(Forum: string.Empty),
                    Llm: new LlmConfig(
                        Provider: string.Empty,
                        Model: string.Empty,
                        BaseUrl: string.Empty,
                        ApiKeyEnvVar: string.Empty,
                        Temperature: 0
                    ),
                    Orleans: new OrleansConfig(
                        ClusterId: string.Empty,
                        ServiceId: string.Empty,
                        GatewayHost: string.Empty,
                        GatewayPort: 0
                    )
                );
        }

        return Task.FromResult(new ShowConfigResult(Config: config));
    }
}
