using GitForest.Core.Services;

namespace GitForest.Application.Features.Plans;

/// <summary>
/// Routes reconciliation forum execution based on config default + per-request overrides.
/// </summary>
public interface IReconciliationForumRouter
{
    Task<ReconciliationStrategy> RunAsync(
        ReconcileContext context,
        string? forumOverride,
        CancellationToken cancellationToken = default);
}

