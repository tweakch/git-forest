using GitForest.Cli;
using MediatR;

namespace GitForest.Cli.Features.Config;

public sealed record ShowConfigQuery(bool Effective) : IRequest<ShowConfigResult>;

public sealed record ShowConfigResult(ForestConfig Config);

internal sealed class ShowConfigHandler : IRequestHandler<ShowConfigQuery, ShowConfigResult>
{
    public Task<ShowConfigResult> Handle(
        ShowConfigQuery request,
        CancellationToken cancellationToken
    )
    {
        _ = request;
        _ = cancellationToken;

        var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);

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
                    )
                );
        }

        return Task.FromResult(new ShowConfigResult(Config: config));
    }
}
