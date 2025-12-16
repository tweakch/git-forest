using GitForest.Core.Persistence;
using GitForest.Core.Specifications.Plans;
using GitForest.Core.Specifications.Planters;
using MediatR;

namespace GitForest.Application.Features.Planters;

public sealed record ListPlantersQuery(bool IncludeBuiltin, bool IncludeCustom) : IRequest<IReadOnlyList<PlanterRow>>;

public sealed record PlanterRow(string Id, string Kind, string[] Plans);

internal sealed class ListPlantersHandler : IRequestHandler<ListPlantersQuery, IReadOnlyList<PlanterRow>>
{
    private readonly IPlanRepository _plans;
    private readonly IPlanterRepository _planters;

    public ListPlantersHandler(IPlanRepository plans, IPlanterRepository planters)
    {
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _planters = planters ?? throw new ArgumentNullException(nameof(planters));
    }

    public async Task<IReadOnlyList<PlanterRow>> Handle(ListPlantersQuery request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var rows = new List<PlanterRow>();

        Dictionary<string, HashSet<string>> builtinById = new(StringComparer.OrdinalIgnoreCase);
        if (request.IncludeBuiltin)
        {
            var installed = await _plans.ListAsync(new AllPlansSpec(), cancellationToken);
            foreach (var plan in installed)
            {
                var planId = (plan.Id ?? string.Empty).Trim();
                if (planId.Length == 0) continue;

                foreach (var rawPlanterId in plan.Planters ?? new List<string>())
                {
                    var planterId = (rawPlanterId ?? string.Empty).Trim();
                    if (planterId.Length == 0) continue;

                    if (!builtinById.TryGetValue(planterId, out var referencedBy))
                    {
                        referencedBy = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        builtinById[planterId] = referencedBy;
                    }

                    referencedBy.Add(planId);
                }
            }

            rows.AddRange(builtinById
                .Select(kvp => new PlanterRow(
                    Id: kvp.Key,
                    Kind: "builtin",
                    Plans: kvp.Value.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase));
        }

        if (request.IncludeCustom)
        {
            // Custom planters are stored explicitly in the planter repository.
            var custom = await _planters.ListAsync(new AllPlantersSpec(), cancellationToken);
            foreach (var planter in custom)
            {
                var id = (planter.Id ?? string.Empty).Trim();
                if (id.Length == 0) continue;

                rows.Add(new PlanterRow(Id: id, Kind: "custom", Plans: Array.Empty<string>()));
            }
        }

        // De-duplicate: if a custom planter shares the same id as a builtin, keep builtin.
        var merged = rows
            .GroupBy(r => r.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var builtinRow = g.FirstOrDefault(x => string.Equals(x.Kind, "builtin", StringComparison.OrdinalIgnoreCase));
                return builtinRow ?? g.First();
            })
            .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return merged;
    }
}

public sealed record GetPlanterQuery(string PlanterId) : IRequest<PlanterInfoResult>;

public sealed record PlanterInfoResult(bool Exists, string Id, string Kind, string[] Plans);

internal sealed class GetPlanterHandler : IRequestHandler<GetPlanterQuery, PlanterInfoResult>
{
    private readonly IPlanRepository _plans;
    private readonly IPlanterRepository _planters;

    public GetPlanterHandler(IPlanRepository plans, IPlanterRepository planters)
    {
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _planters = planters ?? throw new ArgumentNullException(nameof(planters));
    }

    public async Task<PlanterInfoResult> Handle(GetPlanterQuery request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var id = (request.PlanterId ?? string.Empty).Trim();
        if (id.Length == 0)
        {
            return new PlanterInfoResult(false, string.Empty, string.Empty, Array.Empty<string>());
        }

        var plans = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var installedPlans = await _plans.ListAsync(new AllPlansSpec(), cancellationToken);
        foreach (var plan in installedPlans)
        {
            var planId = (plan.Id ?? string.Empty).Trim();
            if (planId.Length == 0) continue;

            foreach (var raw in plan.Planters ?? new List<string>())
            {
                if (string.Equals((raw ?? string.Empty).Trim(), id, StringComparison.OrdinalIgnoreCase))
                {
                    plans.Add(planId);
                }
            }
        }

        var isBuiltin = plans.Count > 0;
        var isCustom = await _planters.GetByIdAsync(id, cancellationToken) is not null;
        var exists = isBuiltin || isCustom;
        var kind = isBuiltin ? "builtin" : (isCustom ? "custom" : string.Empty);

        return new PlanterInfoResult(
            Exists: exists,
            Id: id,
            Kind: kind,
            Plans: plans.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray());
    }
}

