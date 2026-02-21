using GitForest.Core.Persistence;
using GitForest.Core.Services;
using GitForest.Core.Specifications.Plans;
using GitForest.Mediator;

namespace GitForest.Application.Features.Forest;

public sealed record GetForestStatusLiteQuery() : IRequest<ForestStatusLiteResult>;

public sealed record ForestStatusLiteResult(
    int PlansCount,
    string[] PlantersAvailable,
    string[] PlannersAvailable,
    string LockStatus
);

internal sealed class GetForestStatusLiteHandler
    : IRequestHandler<GetForestStatusLiteQuery, ForestStatusLiteResult>
{
    private readonly IPlanRepository _plans;
    private readonly ILockStatusProvider _lockStatus;

    public GetForestStatusLiteHandler(IPlanRepository plans, ILockStatusProvider lockStatus)
    {
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _lockStatus = lockStatus ?? throw new ArgumentNullException(nameof(lockStatus));
    }

    public async Task<ForestStatusLiteResult> Handle(
        GetForestStatusLiteQuery request,
        CancellationToken cancellationToken
    )
    {
        _ = request;

        var plans = await _plans.ListAsync(new AllPlansSpec(), cancellationToken);

        var plantersAvailable = plans
            .SelectMany(p => p.Planters ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var plannersAvailable = plans
            .SelectMany(p => p.Planners ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var lockStatus = _lockStatus.GetLockStatus();

        return new ForestStatusLiteResult(
            PlansCount: plans.Count,
            PlantersAvailable: plantersAvailable,
            PlannersAvailable: plannersAvailable,
            LockStatus: lockStatus
        );
    }
}
