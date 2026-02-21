using GitForest.Core.Persistence;
using GitForest.Core.Specifications.Plans;
using GitForest.Mediator;

namespace GitForest.Application.Features.Planners;

public sealed record ListPlannersQuery(string? PlanFilter) : IRequest<IReadOnlyList<PlannerRow>>;

public sealed record PlannerRow(string Id, string[] Plans);

internal sealed class ListPlannersHandler
    : IRequestHandler<ListPlannersQuery, IReadOnlyList<PlannerRow>>
{
    private readonly IPlanRepository _plans;

    public ListPlannersHandler(IPlanRepository plans)
    {
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
    }

    public async Task<IReadOnlyList<PlannerRow>> Handle(
        ListPlannersQuery request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var installed = await _plans.ListAsync(new AllPlansSpec(), cancellationToken);
        if (!string.IsNullOrWhiteSpace(request.PlanFilter))
        {
            var planId = request.PlanFilter.Trim();
            installed = installed
                .Where(p => string.Equals(p.Id, planId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        var planners = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var plan in installed)
        {
            var planId = (plan.Id ?? string.Empty).Trim();
            if (planId.Length == 0)
                continue;

            foreach (var rawPlannerId in plan.Planners ?? new List<string>())
            {
                var plannerId = (rawPlannerId ?? string.Empty).Trim();
                if (plannerId.Length == 0)
                    continue;

                if (!planners.TryGetValue(plannerId, out var referencedBy))
                {
                    referencedBy = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    planners[plannerId] = referencedBy;
                }

                referencedBy.Add(planId);
            }
        }

        var rows = planners
            .Select(kvp => new PlannerRow(
                Id: kvp.Key,
                Plans: kvp.Value.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()
            ))
            .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return rows;
    }
}
