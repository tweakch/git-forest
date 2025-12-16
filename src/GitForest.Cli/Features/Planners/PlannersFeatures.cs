using MediatR;
using GitForest.Infrastructure.FileSystem.Serialization;

namespace GitForest.Cli.Features.Planners;

public sealed record ListPlannersQuery(string? PlanFilter) : IRequest<IReadOnlyList<PlannerRow>>;

public sealed record PlannerRow(string Id, string[] Plans);

internal sealed class ListPlannersHandler : IRequestHandler<ListPlannersQuery, IReadOnlyList<PlannerRow>>
{
    public Task<IReadOnlyList<PlannerRow>> Handle(ListPlannersQuery request, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);

        var plans = ForestStore.ListPlans(forestDir);
        if (!string.IsNullOrWhiteSpace(request.PlanFilter))
        {
            var planId = request.PlanFilter.Trim();
            plans = plans.Where(p => string.Equals(p.Id, planId, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        // Aggregate unique planners across installed plans, also tracking which plan(s) reference each planner.
        var planners = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var installed in plans)
        {
            if (string.IsNullOrWhiteSpace(installed.Id))
            {
                continue;
            }

            var planYamlPath = Path.Combine(forestDir, "plans", installed.Id.Trim(), "plan.yaml");
            if (!File.Exists(planYamlPath))
            {
                continue;
            }

            try
            {
                var yaml = File.ReadAllText(planYamlPath);
                var parsed = PlanYamlLite.Parse(yaml);
                foreach (var rawPlannerId in parsed.Planners ?? Array.Empty<string>())
                {
                    if (string.IsNullOrWhiteSpace(rawPlannerId))
                    {
                        continue;
                    }

                    var plannerId = rawPlannerId.Trim();
                    if (!planners.TryGetValue(plannerId, out var referencedByPlans))
                    {
                        referencedByPlans = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        planners[plannerId] = referencedByPlans;
                    }

                    referencedByPlans.Add(installed.Id.Trim());
                }
            }
            catch
            {
                // best-effort: ignore invalid plan YAML
            }
        }

        var rows = planners
            .Select(kvp => new PlannerRow(
                Id: kvp.Key,
                Plans: kvp.Value.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()))
            .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult((IReadOnlyList<PlannerRow>)rows);
    }
}

