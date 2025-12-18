namespace GitForest.Infrastructure.FileSystem;

internal sealed class FileSystemForestPaths
{
    private readonly string _forestDir;

    public FileSystemForestPaths(string forestDir)
    {
        _forestDir = string.IsNullOrWhiteSpace(forestDir)
            ? throw new ArgumentException("Forest directory must be provided.", nameof(forestDir))
            : forestDir.Trim();
    }

    public string ForestDir => _forestDir;

    public string ForestYamlPath => Path.Combine(_forestDir, "forest.yaml");
    public string ConfigYamlPath => Path.Combine(_forestDir, "config.yaml");
    public string LockPath => Path.Combine(_forestDir, "lock");

    public string PlansDir => Path.Combine(_forestDir, "plans");
    public string PlantsDir => Path.Combine(_forestDir, "plants");
    public string PlantersDir => Path.Combine(_forestDir, "planters");
    public string PlannersDir => Path.Combine(_forestDir, "planners");
    public string LogsDir => Path.Combine(_forestDir, "logs");

    public string PlanDir(string planId) => Path.Combine(PlansDir, planId);

    public string PlanYamlPath(string planId) => Path.Combine(PlanDir(planId), "plan.yaml");

    public string PlanInstallJsonPath(string planId) =>
        Path.Combine(PlanDir(planId), "install.json");

    public string PlantDirFromKey(string plantKey)
    {
        var (planId, slug) = SplitPlantKey(plantKey);
        return Path.Combine(PlantsDir, $"{planId}__{slug}");
    }

    public string PlantYamlPathFromKey(string plantKey) =>
        Path.Combine(PlantDirFromKey(plantKey), "plant.yaml");

    public string PlanterDir(string planterId) => Path.Combine(PlantersDir, planterId);

    public string PlanterYamlPath(string planterId) =>
        Path.Combine(PlanterDir(planterId), "planter.yaml");

    public string PlannerDir(string plannerId) => Path.Combine(PlannersDir, plannerId);

    public string PlannerYamlPath(string plannerId) =>
        Path.Combine(PlannerDir(plannerId), "planner.yaml");

    public static (string PlanId, string Slug) SplitPlantKey(string key)
    {
        var k = (key ?? string.Empty).Trim();
        var idx = k.IndexOf(':', StringComparison.Ordinal);
        if (idx <= 0 || idx == k.Length - 1)
        {
            throw new InvalidDataException(
                $"Invalid plant key '{key}'. Expected format: <plan-id>:<plant-slug>."
            );
        }

        var planId = k[..idx].Trim();
        var slug = k[(idx + 1)..].Trim();
        if (planId.Length == 0 || slug.Length == 0)
        {
            throw new InvalidDataException(
                $"Invalid plant key '{key}'. Expected format: <plan-id>:<plant-slug>."
            );
        }

        return (planId, slug);
    }
}

