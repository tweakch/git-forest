using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GitForest.Cli;

internal static class ForestStore
{
    internal const string DefaultForestDirName = ".git-forest";

    public static string GetForestDir(string? dirOptionValue, string? workingDirectory = null)
    {
        var dir = string.IsNullOrWhiteSpace(dirOptionValue) ? DefaultForestDirName : dirOptionValue.Trim();

        if (Path.IsPathRooted(dir))
        {
            return dir;
        }

        var cwd = string.IsNullOrWhiteSpace(workingDirectory) ? Environment.CurrentDirectory : workingDirectory;
        return Path.GetFullPath(Path.Combine(cwd, dir));
    }

    public static bool IsInitialized(string forestDir)
    {
        if (!Directory.Exists(forestDir))
        {
            return false;
        }

        return File.Exists(Path.Combine(forestDir, "forest.yaml"));
    }

    public static void Initialize(string forestDir)
    {
        Directory.CreateDirectory(forestDir);

        // Required folders (CLI.md §12)
        Directory.CreateDirectory(Path.Combine(forestDir, "plans"));
        Directory.CreateDirectory(Path.Combine(forestDir, "plants"));
        Directory.CreateDirectory(Path.Combine(forestDir, "planters"));
        Directory.CreateDirectory(Path.Combine(forestDir, "planners"));
        Directory.CreateDirectory(Path.Combine(forestDir, "logs"));

        // Required files
        var forestYamlPath = Path.Combine(forestDir, "forest.yaml");
        if (!File.Exists(forestYamlPath))
        {
            var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            File.WriteAllText(
                forestYamlPath,
                $"version: v0{Environment.NewLine}initialized_at: {now}{Environment.NewLine}",
                Encoding.UTF8);
        }

        var configYamlPath = Path.Combine(forestDir, "config.yaml");
        if (!File.Exists(configYamlPath))
        {
            File.WriteAllText(
                configYamlPath,
                $"# Repo-level git-forest config{Environment.NewLine}locks:{Environment.NewLine}  timeoutSeconds: 15{Environment.NewLine}",
                Encoding.UTF8);
        }

        var lockPath = Path.Combine(forestDir, "lock");
        if (!File.Exists(lockPath))
        {
            File.WriteAllText(lockPath, string.Empty, Encoding.UTF8);
        }
    }

    public static InstalledPlan InstallPlan(string forestDir, string source)
    {
        if (!IsInitialized(forestDir))
        {
            throw new ForestNotInitializedException(forestDir);
        }

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
            throw new PlanSourceNotFoundException(source);
        }

        var yaml = File.ReadAllText(resolvedSource, Encoding.UTF8);
        var plan = PlanYamlLite.Parse(yaml);

        if (string.IsNullOrWhiteSpace(plan.Id))
        {
            throw new InvalidDataException($"Plan YAML at '{resolvedSource}' is missing required top-level 'id'.");
        }

        var planDir = Path.Combine(forestDir, "plans", plan.Id);
        Directory.CreateDirectory(planDir);

        var destPlanYaml = Path.Combine(planDir, "plan.yaml");
        File.WriteAllText(destPlanYaml, yaml, Encoding.UTF8);

        var installedAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var sha256 = ComputeSha256Hex(Encoding.UTF8.GetBytes(yaml));

        var installMetadataPath = Path.Combine(planDir, "install.json");
        var metadata = new
        {
            id = plan.Id,
            name = plan.Name,
            version = plan.Version,
            category = plan.Category,
            author = plan.Author,
            license = plan.License,
            repository = plan.Repository,
            homepage = plan.Homepage,
            source = source,
            installedAt,
            sha256
        };
        File.WriteAllText(installMetadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8);

        return new InstalledPlan(
            Id: plan.Id,
            Name: plan.Name,
            Version: plan.Version,
            Category: plan.Category,
            Author: plan.Author,
            License: plan.License,
            Repository: plan.Repository,
            Homepage: plan.Homepage,
            Source: source,
            InstalledAt: installedAt,
            Sha256: sha256);
    }

    public static IReadOnlyList<InstalledPlan> ListPlans(string forestDir)
    {
        if (!IsInitialized(forestDir))
        {
            throw new ForestNotInitializedException(forestDir);
        }

        var plansDir = Path.Combine(forestDir, "plans");
        if (!Directory.Exists(plansDir))
        {
            return Array.Empty<InstalledPlan>();
        }

        var results = new List<InstalledPlan>();
        foreach (var dir in Directory.GetDirectories(plansDir))
        {
            var planYamlPath = Path.Combine(dir, "plan.yaml");
            if (!File.Exists(planYamlPath))
            {
                continue;
            }

            var yaml = File.ReadAllText(planYamlPath, Encoding.UTF8);
            var parsed = PlanYamlLite.Parse(yaml);
            var id = string.IsNullOrWhiteSpace(parsed.Id) ? Path.GetFileName(dir) : parsed.Id;

            var source = string.Empty;
            var installedAt = string.Empty;
            var sha256 = string.Empty;
            var installJsonPath = Path.Combine(dir, "install.json");
            if (File.Exists(installJsonPath))
            {
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(installJsonPath, Encoding.UTF8));
                    if (doc.RootElement.TryGetProperty("source", out var src))
                    {
                        source = src.GetString() ?? string.Empty;
                    }

                    if (doc.RootElement.TryGetProperty("installedAt", out var iat))
                    {
                        installedAt = iat.GetString() ?? string.Empty;
                    }

                    if (doc.RootElement.TryGetProperty("sha256", out var sh))
                    {
                        sha256 = sh.GetString() ?? string.Empty;
                    }
                }
                catch
                {
                    // best-effort metadata
                }
            }

            if (string.IsNullOrWhiteSpace(sha256))
            {
                sha256 = ComputeSha256Hex(Encoding.UTF8.GetBytes(yaml));
            }

            results.Add(new InstalledPlan(
                Id: id,
                Name: parsed.Name,
                Version: parsed.Version,
                Category: parsed.Category,
                Author: parsed.Author,
                License: parsed.License,
                Repository: parsed.Repository,
                Homepage: parsed.Homepage,
                Source: source,
                InstalledAt: installedAt,
                Sha256: sha256));
        }

        return results
            .OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static ReconcileResult ReconcilePlan(string forestDir, string planId, bool dryRun)
    {
        if (!IsInitialized(forestDir))
        {
            throw new ForestNotInitializedException(forestDir);
        }

        if (string.IsNullOrWhiteSpace(planId))
        {
            throw new ArgumentException("Plan ID must be provided.", nameof(planId));
        }

        var planDir = Path.Combine(forestDir, "plans", planId);
        var planYamlPath = Path.Combine(planDir, "plan.yaml");
        if (!File.Exists(planYamlPath))
        {
            throw new PlanNotInstalledException(planId);
        }

        var planYaml = File.ReadAllText(planYamlPath, Encoding.UTF8);
        var plan = PlanYamlLite.Parse(planYaml);

        var plantsDir = Path.Combine(forestDir, "plants");
        Directory.CreateDirectory(plantsDir);

        var templates = plan.PlantTemplateNames.Count > 0 ? plan.PlantTemplateNames : new List<string> { "default-plant" };
        var planners = plan.Planners.Count > 0 ? plan.Planners : new List<string> { "default-planner" };
        var planters = plan.Planters.Count > 0 ? plan.Planters : new List<string>();

        var created = 0;
        var updated = 0;

        for (var i = 0; i < templates.Count; i++)
        {
            var slug = NormalizeSlug(templates[i]);
            var key = $"{planId}:{slug}";
            var dirName = $"{planId}__{slug}";
            var plantDir = Path.Combine(plantsDir, dirName);
            var plantYamlPath = Path.Combine(plantDir, "plant.yaml");

            var plannerId = planners[i % planners.Count];
            var assignedPlanters = planters.Count > 0 ? new[] { planters[i % planters.Count] } : Array.Empty<string>();

            var plant = new PlantRecord(
                Key: key,
                Status: "planned",
                Title: $"{plan.Name}".Trim() == string.Empty ? slug : $"{plan.Name}: {slug}",
                PlanId: planId,
                PlannerId: plannerId,
                AssignedPlanters: assignedPlanters);

            if (!Directory.Exists(plantDir) || !File.Exists(plantYamlPath))
            {
                if (!dryRun)
                {
                    Directory.CreateDirectory(plantDir);
                    File.WriteAllText(plantYamlPath, PlantYamlLite.Serialize(plant), Encoding.UTF8);
                }

                created++;
            }
            else
            {
                // For now, keep reconcile minimal: treat existing as up-to-date.
                updated++;
            }
        }

        return new ReconcileResult(planId, created, updated);
    }

    public static IReadOnlyList<PlantRecord> ListPlants(string forestDir, string? statusFilter, string? planFilter)
    {
        if (!IsInitialized(forestDir))
        {
            throw new ForestNotInitializedException(forestDir);
        }

        var plantsDir = Path.Combine(forestDir, "plants");
        if (!Directory.Exists(plantsDir))
        {
            return Array.Empty<PlantRecord>();
        }

        var plants = new List<PlantRecord>();
        foreach (var dir in Directory.GetDirectories(plantsDir))
        {
            var plantYamlPath = Path.Combine(dir, "plant.yaml");
            if (!File.Exists(plantYamlPath))
            {
                continue;
            }

            var yaml = File.ReadAllText(plantYamlPath, Encoding.UTF8);
            var plant = PlantYamlLite.Parse(yaml);
            plants.Add(plant);
        }

        IEnumerable<PlantRecord> filtered = plants;
        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            filtered = filtered.Where(p => string.Equals(p.Status, statusFilter.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(planFilter))
        {
            filtered = filtered.Where(p => string.Equals(p.PlanId, planFilter.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        return filtered
            .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string NormalizeSlug(string input)
    {
        var trimmed = (input ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return "untitled";
        }

        // Keep it deterministic and file-system safe.
        var sb = new StringBuilder(trimmed.Length);
        var lastWasDash = false;
        foreach (var ch in trimmed)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
                lastWasDash = false;
                continue;
            }

            if (ch is '-' or '_' or ' ' or '.')
            {
                if (!lastWasDash)
                {
                    sb.Append('-');
                    lastWasDash = true;
                }
            }
        }

        var slug = sb.ToString().Trim('-');
        return slug.Length == 0 ? "untitled" : slug;
    }

    public sealed record InstalledPlan(
        string Id,
        string Name,
        string Version,
        string Category,
        string Author,
        string License,
        string Repository,
        string Homepage,
        string Source,
        string InstalledAt,
        string Sha256);
    public sealed record ReconcileResult(string PlanId, int PlantsCreated, int PlantsUpdated);
    public sealed record PlantRecord(string Key, string Status, string Title, string PlanId, string? PlannerId, IReadOnlyList<string> AssignedPlanters);

    private static string ComputeSha256Hex(ReadOnlySpan<byte> bytes)
    {
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public sealed class ForestNotInitializedException : Exception
    {
        public ForestNotInitializedException(string forestDir) : base($"Forest not initialized at '{forestDir}'.") { }
    }

    public sealed class PlanSourceNotFoundException : Exception
    {
        public PlanSourceNotFoundException(string source) : base($"Plan source not found: '{source}'.") { }
    }

    public sealed class PlanNotInstalledException : Exception
    {
        public PlanNotInstalledException(string planId) : base($"Plan not installed: '{planId}'.") { }
    }
}

internal static class PlanYamlLite
{
    public sealed record ParsedPlan(
        string Id,
        string Name,
        string Version,
        string Category,
        string Author,
        string License,
        string Repository,
        string Homepage,
        IReadOnlyList<string> Planners,
        IReadOnlyList<string> Planters,
        IReadOnlyList<string> PlantTemplateNames);

    public static ParsedPlan Parse(string yaml)
    {
        var id = string.Empty;
        var name = string.Empty;
        var version = string.Empty;
        var category = string.Empty;
        var author = string.Empty;
        var license = string.Empty;
        var repository = string.Empty;
        var homepage = string.Empty;
        var planners = new List<string>();
        var planters = new List<string>();
        var templateNames = new List<string>();

        var lines = SplitLines(yaml);

        string? currentList = null;
        var inPlantTemplates = false;

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            if (!char.IsWhiteSpace(line[0]))
            {
                inPlantTemplates = false;
                currentList = null;

                if (TryParseTopLevelScalar(line, "id", out var v)) { id = v; continue; }
                if (TryParseTopLevelScalar(line, "name", out v)) { name = v; continue; }
                if (TryParseTopLevelScalar(line, "version", out v)) { version = v; continue; }
                if (TryParseTopLevelScalar(line, "category", out v)) { category = v; continue; }
                if (TryParseTopLevelScalar(line, "author", out v)) { author = v; continue; }
                if (TryParseTopLevelScalar(line, "license", out v)) { license = v; continue; }
                if (TryParseTopLevelScalar(line, "repository", out v)) { repository = v; continue; }
                if (TryParseTopLevelScalar(line, "homepage", out v)) { homepage = v; continue; }

                if (IsTopLevelKey(line, "planners")) { currentList = "planners"; continue; }
                if (IsTopLevelKey(line, "planters")) { currentList = "planters"; continue; }
                if (IsTopLevelKey(line, "plant_templates")) { inPlantTemplates = true; continue; }

                continue;
            }

            if (currentList is not null)
            {
                if (TryParseListItem(line, out var item))
                {
                    if (currentList == "planners") planners.Add(item);
                    if (currentList == "planters") planters.Add(item);
                }

                continue;
            }

            if (inPlantTemplates)
            {
                // We only need the template names for deterministic seed plants.
                // Supports shapes like:
                //   - name: add-integration-tests
                //     title_template: ...
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("- ", StringComparison.Ordinal))
                {
                    trimmed = trimmed[2..].TrimStart();
                }

                if (TryParseTopLevelScalar(trimmed, "name", out var tname) && !string.IsNullOrWhiteSpace(tname))
                {
                    templateNames.Add(tname);
                }
            }
        }

        return new ParsedPlan(
            Id: id,
            Name: name,
            Version: version,
            Category: category,
            Author: author,
            License: license,
            Repository: repository,
            Homepage: homepage,
            Planners: planners,
            Planters: planters,
            PlantTemplateNames: templateNames);
    }

    private static bool IsTopLevelKey(string line, string key)
    {
        return line.StartsWith($"{key}:", StringComparison.Ordinal);
    }

    private static bool TryParseTopLevelScalar(string line, string key, out string value)
    {
        value = string.Empty;
        if (!line.StartsWith($"{key}:", StringComparison.Ordinal))
        {
            return false;
        }

        value = line[(key.Length + 1)..].Trim();
        // Drop wrapping quotes if present.
        if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            value = value[1..^1];
        }

        return true;
    }

    private static bool TryParseListItem(string line, out string value)
    {
        value = string.Empty;
        var trimmed = line.TrimStart();
        if (!trimmed.StartsWith("- ", StringComparison.Ordinal))
        {
            return false;
        }

        value = trimmed[2..].Trim();
        if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            value = value[1..^1];
        }

        return value.Length > 0;
    }

    private static string[] SplitLines(string yaml)
    {
        return yaml.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
    }
}

internal static class PlantYamlLite
{
    public static string Serialize(ForestStore.PlantRecord plant)
    {
        // Minimal plant.yaml aligned with docs/forest-maintenance-contract.md
        var sb = new StringBuilder();
        sb.Append("key: ").Append(plant.Key).AppendLine();
        sb.Append("status: ").Append(string.IsNullOrWhiteSpace(plant.Status) ? "planned" : plant.Status).AppendLine();
        sb.Append("title: ").Append(EscapeScalar(plant.Title)).AppendLine();
        sb.Append("plan_id: ").Append(plant.PlanId).AppendLine();

        if (!string.IsNullOrWhiteSpace(plant.PlannerId))
        {
            sb.AppendLine("context:");
            sb.Append("  planner: ").Append(plant.PlannerId).AppendLine();
        }

        sb.AppendLine("assigned_planters:");
        foreach (var planter in plant.AssignedPlanters ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(planter))
            {
                sb.Append("  - ").Append(planter.Trim()).AppendLine();
            }
        }

        sb.AppendLine("branches: []");
        sb.Append("created_at: ").Append(DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)).AppendLine();
        return sb.ToString();
    }

    public static ForestStore.PlantRecord Parse(string yaml)
    {
        var key = string.Empty;
        var status = "planned";
        var title = string.Empty;
        var planId = string.Empty;
        string? plannerId = null;
        var assignedPlanters = new List<string>();

        var lines = yaml.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');

        string? currentList = null;
        var inContext = false;

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            if (!char.IsWhiteSpace(line[0]))
            {
                currentList = null;
                inContext = false;

                if (TryParseScalar(line, "key", out var v)) { key = v; continue; }
                if (TryParseScalar(line, "status", out v)) { status = v; continue; }
                if (TryParseScalar(line, "title", out v)) { title = v; continue; }
                if (TryParseScalar(line, "plan_id", out v)) { planId = v; continue; }
                if (line.StartsWith("assigned_planters:", StringComparison.Ordinal)) { currentList = "assigned_planters"; continue; }
                if (line.StartsWith("context:", StringComparison.Ordinal)) { inContext = true; continue; }
                continue;
            }

            if (inContext)
            {
                var trimmed = line.TrimStart();
                if (TryParseScalar(trimmed, "planner", out var p))
                {
                    plannerId = p;
                }

                continue;
            }

            if (currentList == "assigned_planters")
            {
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("- ", StringComparison.Ordinal))
                {
                    var item = trimmed[2..].Trim();
                    if (item.Length > 0)
                    {
                        assignedPlanters.Add(Unquote(item));
                    }
                }
            }
        }

        // Best-effort fallbacks
        if (string.IsNullOrWhiteSpace(planId) && !string.IsNullOrWhiteSpace(key))
        {
            var idx = key.IndexOf(':');
            if (idx > 0)
            {
                planId = key[..idx];
            }
        }

        return new ForestStore.PlantRecord(
            Key: key,
            Status: status,
            Title: title,
            PlanId: planId,
            PlannerId: plannerId,
            AssignedPlanters: assignedPlanters);
    }

    private static bool TryParseScalar(string line, string key, out string value)
    {
        value = string.Empty;
        if (!line.StartsWith($"{key}:", StringComparison.Ordinal))
        {
            return false;
        }

        value = Unquote(line[(key.Length + 1)..].Trim());
        return true;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }

    private static string EscapeScalar(string value)
    {
        var v = value ?? string.Empty;
        if (v.Contains(':') || v.Contains('#') || v.Contains('"') || v.Contains('\'') || v.Contains('\\'))
        {
            // YAML double-quoted scalar (minimal escaping)
            var escaped = v.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
            return $"\"{escaped}\"";
        }

        return v;
    }
}

