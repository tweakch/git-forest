using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GitForest.Infrastructure.FileSystem.Serialization;

namespace GitForest.Cli;

internal static class ForestStore
{
    internal const string DefaultForestDirName = ".git-forest";
    private const string DefaultStatus = "planned";

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
                $"# Repo-level git-forest config{Environment.NewLine}persistence:{Environment.NewLine}  provider: {ForestConfigReader.DefaultPersistenceProvider}{Environment.NewLine}locks:{Environment.NewLine}  timeoutSeconds: {ForestConfigReader.DefaultLocksTimeoutSeconds}{Environment.NewLine}",
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

            var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var plant = new PlantRecord(
                Key: key,
                Status: "planned",
                Title: $"{plan.Name}".Trim() == string.Empty ? slug : $"{plan.Name}: {slug}",
                PlanId: planId,
                PlannerId: plannerId,
                AssignedPlanters: assignedPlanters,
                Branches: Array.Empty<string>(),
                CreatedAt: now,
                UpdatedAt: null);

            if (!Directory.Exists(plantDir) || !File.Exists(plantYamlPath))
            {
                if (!dryRun)
                {
                    Directory.CreateDirectory(plantDir);
                    File.WriteAllText(plantYamlPath, PlantYamlLite.Serialize(ToFileModel(plant)), Encoding.UTF8);
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
            var plant = FromFileModel(PlantYamlLite.Parse(yaml));
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

    public static PlantRecord ResolvePlant(string forestDir, string selector)
    {
        if (!IsInitialized(forestDir))
        {
            throw new ForestNotInitializedException(forestDir);
        }

        var plants = ListPlants(forestDir, statusFilter: null, planFilter: null);
        var matches = FindMatches(plants, selector);
        if (matches.Count == 1)
        {
            return matches[0];
        }

        if (matches.Count == 0)
        {
            throw new PlantNotFoundException(selector);
        }

        throw new PlantAmbiguousSelectorException(selector, matches.Select(p => p.Key).ToArray());
    }

    public static PlantRecord UpdatePlant(string forestDir, string selector, Func<PlantRecord, PlantRecord> update, bool dryRun)
    {
        if (update is null)
        {
            throw new ArgumentNullException(nameof(update));
        }

        var plant = ResolvePlant(forestDir, selector);
        var updated = update(plant);
        if (!dryRun)
        {
            WritePlant(forestDir, updated with { UpdatedAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture) });
        }

        return updated;
    }

    public static void WritePlant(string forestDir, PlantRecord plant)
    {
        if (!IsInitialized(forestDir))
        {
            throw new ForestNotInitializedException(forestDir);
        }

        var (planId, slug) = SplitPlantKey(plant.Key);
        var plantsDir = Path.Combine(forestDir, "plants");
        Directory.CreateDirectory(plantsDir);
        var plantDir = Path.Combine(plantsDir, $"{planId}__{slug}");
        Directory.CreateDirectory(plantDir);
        var plantYamlPath = Path.Combine(plantDir, "plant.yaml");

        var createdAt = plant.CreatedAt;
        if (string.IsNullOrWhiteSpace(createdAt))
        {
            createdAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        }

        File.WriteAllText(
            plantYamlPath,
            PlantYamlLite.Serialize(ToFileModel(plant with { CreatedAt = createdAt })),
            Encoding.UTF8);
    }

    public static (string PlanId, string Slug) SplitPlantKey(string key)
    {
        var k = (key ?? string.Empty).Trim();
        var idx = k.IndexOf(':', StringComparison.Ordinal);
        if (idx <= 0 || idx == k.Length - 1)
        {
            throw new InvalidDataException($"Invalid plant key '{key}'. Expected format: <plan-id>:<plant-slug>.");
        }

        var planId = k[..idx].Trim();
        var slug = k[(idx + 1)..].Trim();
        if (planId.Length == 0 || slug.Length == 0)
        {
            throw new InvalidDataException($"Invalid plant key '{key}'. Expected format: <plan-id>:<plant-slug>.");
        }

        return (planId, slug);
    }

    private static IReadOnlyList<PlantRecord> FindMatches(IReadOnlyList<PlantRecord> plants, string selector)
    {
        var sel = (selector ?? string.Empty).Trim();
        if (sel.Length == 0 || plants.Count == 0)
        {
            return Array.Empty<PlantRecord>();
        }

        // 1) Exact key match: <plan-id>:<slug>
        var exact = plants.Where(p => string.Equals(p.Key, sel, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (exact.Length > 0)
        {
            return exact;
        }

        // 2) Pxx style stable index into ordered list (best-effort; deterministic ordering).
        if (TryParsePIndex(sel, out var index))
        {
            var ordered = plants.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase).ToArray();
            if (index >= 0 && index < ordered.Length)
            {
                return new[] { ordered[index] };
            }

            return Array.Empty<PlantRecord>();
        }

        // 3) Slug match: match any plant whose key right-side equals selector.
        var slugMatches = plants.Where(p =>
        {
            var key = p.Key ?? string.Empty;
            var idx = key.IndexOf(':', StringComparison.Ordinal);
            if (idx < 0 || idx == key.Length - 1)
            {
                return false;
            }

            var slug = key[(idx + 1)..];
            return string.Equals(slug, sel, StringComparison.OrdinalIgnoreCase);
        }).ToArray();

        return slugMatches;
    }

    private static bool TryParsePIndex(string selector, out int index)
    {
        // Accept P01, p1, P0003 → 1-based ordinal; convert to 0-based index.
        index = -1;
        if (selector.Length < 2)
        {
            return false;
        }

        if (selector[0] != 'p' && selector[0] != 'P')
        {
            return false;
        }

        var digits = selector[1..].Trim();
        if (digits.Length == 0)
        {
            return false;
        }

        foreach (var ch in digits)
        {
            if (!char.IsDigit(ch))
            {
                return false;
            }
        }

        if (!int.TryParse(digits, out var oneBased) || oneBased <= 0)
        {
            return false;
        }

        index = oneBased - 1;
        return true;
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

    private static PlantFileModel ToFileModel(PlantRecord plant)
    {
        return new PlantFileModel(
            Key: plant.Key,
            Status: plant.Status,
            Title: plant.Title,
            PlanId: plant.PlanId,
            PlannerId: plant.PlannerId,
            AssignedPlanters: plant.AssignedPlanters ?? Array.Empty<string>(),
            Branches: plant.Branches ?? Array.Empty<string>(),
            CreatedAt: plant.CreatedAt,
            UpdatedAt: plant.UpdatedAt,
            Description: null);
    }

    private static PlantRecord FromFileModel(PlantFileModel plant)
    {
        return new PlantRecord(
            Key: plant.Key,
            Status: plant.Status,
            Title: plant.Title,
            PlanId: plant.PlanId,
            PlannerId: plant.PlannerId,
            AssignedPlanters: plant.AssignedPlanters ?? Array.Empty<string>(),
            Branches: plant.Branches ?? Array.Empty<string>(),
            CreatedAt: plant.CreatedAt,
            UpdatedAt: plant.UpdatedAt);
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
    public sealed record PlantRecord(
        string Key,
        string Status,
        string Title,
        string PlanId,
        string? PlannerId,
        IReadOnlyList<string> AssignedPlanters,
        IReadOnlyList<string> Branches,
        string CreatedAt,
        string? UpdatedAt);

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

    public sealed class PlantNotFoundException : Exception
    {
        public PlantNotFoundException(string selector) : base($"Plant not found: '{selector}'.") { }
    }

    public sealed class PlantAmbiguousSelectorException : Exception
    {
        public string Selector { get; }
        public string[] Matches { get; }

        public PlantAmbiguousSelectorException(string selector, string[] matches)
            : base($"Plant selector is ambiguous: '{selector}'.")
        {
            Selector = selector;
            Matches = matches ?? Array.Empty<string>();
        }
    }
}
