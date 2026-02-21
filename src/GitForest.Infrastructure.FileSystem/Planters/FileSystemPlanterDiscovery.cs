using GitForest.Core.Services;

namespace GitForest.Infrastructure.FileSystem.Planters;

public sealed class FileSystemPlanterDiscovery : IPlanterDiscovery
{
    private readonly string _forestDir;

    public FileSystemPlanterDiscovery(string forestDir)
    {
        _forestDir = forestDir ?? throw new ArgumentNullException(nameof(forestDir));
    }

    public IReadOnlyList<string> ListCustomPlanterIds()
    {
        var plantersDir = Path.Combine(_forestDir, "planters");
        if (!Directory.Exists(plantersDir))
        {
            return Array.Empty<string>();
        }

        var ids = new List<string>();
        foreach (var dir in Directory.GetDirectories(plantersDir))
        {
            var id = Path.GetFileName(dir);
            if (!string.IsNullOrWhiteSpace(id))
            {
                ids.Add(id.Trim());
            }
        }

        return ids.Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool CustomPlanterExists(string planterId)
    {
        var id = (planterId ?? string.Empty).Trim();
        if (id.Length == 0)
        {
            return false;
        }

        return Directory.Exists(Path.Combine(_forestDir, "planters", id));
    }
}
