using System.Text;
using GitForest.Core.Services;

namespace GitForest.Infrastructure.FileSystem.Planters;

public sealed class DeterministicGrowthApplier : IPlanterGrowthApplier
{
    public void ApplyDeterministicGrowth(string repoRoot, string plantKey, string planterId)
    {
        var root = (repoRoot ?? string.Empty).Trim();
        if (root.Length == 0)
        {
            throw new ArgumentException("Repo root must be provided.", nameof(repoRoot));
        }

        var key = (plantKey ?? string.Empty).Trim();
        var pid = (planterId ?? string.Empty).Trim();
        if (key.Length == 0 || pid.Length == 0)
        {
            throw new ArgumentException("Plant key and planter id must be provided.");
        }

        // MVP: a deterministic, safe change that works in any repo. Keep it small and idempotent.
        var readmePath = Path.Combine(root, "README.md");
        if (!File.Exists(readmePath))
        {
            File.WriteAllText(readmePath, "# Repository\n", Encoding.UTF8);
        }

        var marker = $"<!-- git-forest: {key} (planter={pid}) -->";
        var content = File.ReadAllText(readmePath, Encoding.UTF8);
        if (content.Contains(marker, StringComparison.Ordinal))
        {
            return;
        }

        var updated =
            content.TrimEnd()
            + Environment.NewLine
            + Environment.NewLine
            + marker
            + Environment.NewLine;
        File.WriteAllText(readmePath, updated, Encoding.UTF8);
    }
}
