using GitForest.Infrastructure.FileSystem.Serialization;
using MediatR;

namespace GitForest.Cli.Features.Forest;

public sealed record InitForestCommand(string? DirOptionValue, bool Force)
    : IRequest<InitForestResult>;

public sealed record InitForestResult(string DirectoryOptionValue, string ForestDirPath);

internal sealed class InitForestHandler : IRequestHandler<InitForestCommand, InitForestResult>
{
    public Task<InitForestResult> Handle(
        InitForestCommand request,
        CancellationToken cancellationToken
    )
    {
        _ = cancellationToken;
        _ = request.Force; // init is idempotent today

        var dir = string.IsNullOrWhiteSpace(request.DirOptionValue)
            ? ForestStore.DefaultForestDirName
            : request.DirOptionValue!;
        var forestDir = ForestStore.GetForestDir(dir);
        ForestStore.Initialize(forestDir);

        return Task.FromResult(
            new InitForestResult(DirectoryOptionValue: dir, ForestDirPath: forestDir)
        );
    }
}

public sealed record GetForestStatusQuery() : IRequest<ForestStatusResult>;

public sealed record ForestStatusResult(
    int PlansCount,
    int PlantsCount,
    IReadOnlyDictionary<string, int> PlantsByStatus,
    string[] PlantersAvailable,
    string[] PlantersActive,
    string[] PlannersAvailable,
    string[] PlannersActive,
    string LockStatus
);

internal sealed class GetForestStatusHandler
    : IRequestHandler<GetForestStatusQuery, ForestStatusResult>
{
    public Task<ForestStatusResult> Handle(
        GetForestStatusQuery request,
        CancellationToken cancellationToken
    )
    {
        _ = request;
        _ = cancellationToken;

        var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);

        var plans = ForestStore.ListPlans(forestDir);
        var plants = ForestStore.ListPlants(forestDir, statusFilter: null, planFilter: null);

        var plantsByStatus = plants
            .GroupBy(p => (p.Status ?? string.Empty).Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var (plantersAvailable, plannersAvailable) = LoadAvailablePlantersAndPlanners(
            forestDir,
            plans.Select(p => p.Id)
        );

        var nonArchivedPlants = plants
            .Where(p => !string.Equals(p.Status, "archived", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var plantersActive = nonArchivedPlants
            .SelectMany(p => p.AssignedPlanters ?? Array.Empty<string>())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var plannersActive = nonArchivedPlants
            .Select(p => p.PlannerId)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var lockStatus = GetLockStatus(forestDir);

        return Task.FromResult(
            new ForestStatusResult(
                PlansCount: plans.Count,
                PlantsCount: plants.Count,
                PlantsByStatus: plantsByStatus,
                PlantersAvailable: plantersAvailable,
                PlantersActive: plantersActive,
                PlannersAvailable: plannersAvailable,
                PlannersActive: plannersActive,
                LockStatus: lockStatus
            )
        );
    }

    private static string GetLockStatus(string forestDir)
    {
        var lockPath = Path.Combine(forestDir, "lock");
        var lockStatus = "free";
        try
        {
            if (File.Exists(lockPath))
            {
                var lockText = File.ReadAllText(lockPath).Trim();
                if (!string.IsNullOrWhiteSpace(lockText))
                {
                    lockStatus = "held";
                }
            }
        }
        catch
        {
            // best-effort lock status
        }

        return lockStatus;
    }

    private static (
        string[] plantersAvailable,
        string[] plannersAvailable
    ) LoadAvailablePlantersAndPlanners(string forestDir, IEnumerable<string> planIds)
    {
        var planters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var planners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var planId in planIds)
        {
            if (string.IsNullOrWhiteSpace(planId))
            {
                continue;
            }

            var planYamlPath = Path.Combine(forestDir, "plans", planId.Trim(), "plan.yaml");
            if (!File.Exists(planYamlPath))
            {
                continue;
            }

            try
            {
                var yaml = File.ReadAllText(planYamlPath);
                var parsed = PlanYamlLite.Parse(yaml);

                foreach (var p in parsed.Planters ?? Array.Empty<string>())
                {
                    if (!string.IsNullOrWhiteSpace(p))
                    {
                        planters.Add(p.Trim());
                    }
                }

                foreach (var p in parsed.Planners ?? Array.Empty<string>())
                {
                    if (!string.IsNullOrWhiteSpace(p))
                    {
                        planners.Add(p.Trim());
                    }
                }
            }
            catch
            {
                // best-effort: ignore invalid plan YAML
            }
        }

        return (
            planters.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray(),
            planners.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()
        );
    }
}
