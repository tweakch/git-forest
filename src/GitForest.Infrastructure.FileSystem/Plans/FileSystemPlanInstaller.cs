using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GitForest.Core.Services;
using GitForest.Infrastructure.FileSystem.Serialization;

namespace GitForest.Infrastructure.FileSystem.Plans;

public sealed class FileSystemPlanInstaller : IPlanInstaller
{
    private readonly string _forestDir;

    public FileSystemPlanInstaller(string forestDir)
    {
        _forestDir = forestDir ?? string.Empty;
    }

    public Task<(string planId, string version)> InstallAsync(string source, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Plan source must be provided.", nameof(source));
        }

        var resolvedSource = source;
        if (!Path.IsPathRooted(resolvedSource))
        {
            resolvedSource = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, resolvedSource));
        }

        if (!File.Exists(resolvedSource))
        {
            throw new FileNotFoundException("Plan file not found.", resolvedSource);
        }

        var yaml = File.ReadAllText(resolvedSource, Encoding.UTF8);
        var plan = PlanYamlLite.Parse(yaml);

        if (string.IsNullOrWhiteSpace(plan.Id))
        {
            throw new InvalidDataException($"Plan YAML at '{resolvedSource}' is missing required top-level 'id'.");
        }

        var forestDir = _forestDir.Trim();
        var planId = plan.Id.Trim();
        var planDir = Path.Combine(forestDir, "plans", planId);
        Directory.CreateDirectory(planDir);

        var destPlanYaml = Path.Combine(planDir, "plan.yaml");
        File.WriteAllText(destPlanYaml, yaml, Encoding.UTF8);

        // Materialize agent definitions referenced by this plan (idempotent).
        MaterializePlannerAndPlanterYamlsIfMissing(forestDir, planId, plan.Planners, plan.Planters);

        var installedAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var sha256 = ComputeSha256Hex(Encoding.UTF8.GetBytes(yaml));

        var installMetadataPath = Path.Combine(planDir, "install.json");
        var metadata = new
        {
            id = planId,
            name = plan.Name,
            version = plan.Version,
            category = plan.Category,
            author = plan.Author,
            license = plan.License,
            repository = plan.Repository,
            homepage = plan.Homepage,
            source,
            installedAt,
            sha256
        };
        File.WriteAllText(installMetadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8);

        return Task.FromResult((planId: planId, version: plan.Version ?? string.Empty));
    }

    private static void MaterializePlannerAndPlanterYamlsIfMissing(
        string forestDir,
        string planId,
        IReadOnlyList<string> plannerIds,
        IReadOnlyList<string> planterIds)
    {
        var plannersDir = Path.Combine(forestDir, "planners");
        var plantersDir = Path.Combine(forestDir, "planters");

        foreach (var plannerId in NormalizeIds(plannerIds))
        {
            var dir = Path.Combine(plannersDir, plannerId);
            var path = Path.Combine(dir, "planner.yaml");
            if (File.Exists(path))
            {
                continue;
            }

            Directory.CreateDirectory(dir);

            var model = new PlannerFileModel(
                Id: plannerId,
                Name: string.Empty,
                PlanId: planId,
                Type: "llm",
                Configuration: new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase));

            File.WriteAllText(path, PlannerYamlLite.Serialize(model), Encoding.UTF8);
        }

        foreach (var planterId in NormalizeIds(planterIds))
        {
            var dir = Path.Combine(plantersDir, planterId);
            var path = Path.Combine(dir, "planter.yaml");
            if (File.Exists(path))
            {
                continue;
            }

            Directory.CreateDirectory(dir);

            var model = new PlanterFileModel(
                Id: planterId,
                Name: string.Empty,
                Type: "builtin",
                Origin: "plan",
                AssignedPlants: Array.Empty<string>(),
                IsActive: false);

            File.WriteAllText(path, PlanterYamlLite.Serialize(model), Encoding.UTF8);
        }
    }

    private static IReadOnlyList<string> NormalizeIds(IReadOnlyList<string> ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return Array.Empty<string>();
        }

        var results = new List<string>(ids.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in ids)
        {
            var id = (raw ?? string.Empty).Trim();
            if (id.Length == 0)
            {
                continue;
            }

            if (seen.Add(id))
            {
                results.Add(id);
            }
        }

        return results;
    }

    private static string ComputeSha256Hex(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }
}

