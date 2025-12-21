using GitForest.Application.Configuration;
using GitForest.Application.Features.Plans;
using GitForest.Core.Services;

namespace GitForest.Cli.Reconciliation;

public sealed class ReconciliationForumRouter : IReconciliationForumRouter
{
    private readonly ForestConfig _config;
    private readonly IReconciliationForum _fileForum;
    private readonly IReconciliationForum _aiForum;

    public ReconciliationForumRouter(
        ForestConfig config,
        IReconciliationForum fileForum,
        IReconciliationForum aiForum
    )
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _fileForum = fileForum ?? throw new ArgumentNullException(nameof(fileForum));
        _aiForum = aiForum ?? throw new ArgumentNullException(nameof(aiForum));
    }

    public Task<ReconciliationStrategy> RunAsync(
        ReconcileContext context,
        string? forumOverride,
        CancellationToken cancellationToken = default
    )
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        var requested = NormalizeForum(forumOverride);
        var configured = NormalizeForum(_config.Reconcile?.Forum);

        var selected = requested ?? configured ?? ForestConfigReader.DefaultReconcileForum;
        return selected == "ai"
            ? _aiForum.RunAsync(context, cancellationToken)
            : _fileForum.RunAsync(context, cancellationToken);
    }

    private static string? NormalizeForum(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var v = value.Trim().ToLowerInvariant();
        return v is "ai" or "file" ? v : null;
    }
}
