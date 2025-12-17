using System.Globalization;
using System.Text;
using System.Text.Json;
using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Infrastructure.FileSystem.Serialization;

namespace GitForest.Infrastructure.FileSystem.Repositories;

public sealed class FileSystemPlanRepository : AbstractPlanRepository
{
    private readonly FileSystemForestPaths _paths;

    public FileSystemPlanRepository(string forestDir)
    {
        _paths = new FileSystemForestPaths(forestDir);
    }

    public override Task<Plan?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (string.IsNullOrWhiteSpace(id))
        {
            return Task.FromResult<Plan?>(null);
        }

        var planId = id.Trim();
        var planYamlPath = _paths.PlanYamlPath(planId);
        if (!File.Exists(planYamlPath))
        {
            return Task.FromResult<Plan?>(null);
        }

        var yaml = File.ReadAllText(planYamlPath, Encoding.UTF8);
        var parsed = PlanYamlLite.Parse(yaml);

        var installedAt = TryReadInstalledAt(_paths.PlanInstallJsonPath(planId));
        var source = TryReadPlanSource(_paths.PlanInstallJsonPath(planId));

        var plan = new Plan
        {
            Id = string.IsNullOrWhiteSpace(parsed.Id) ? planId : parsed.Id,
            Version = parsed.Version ?? string.Empty,
            Source = source,
            Author = parsed.Author ?? string.Empty,
            License = parsed.License ?? string.Empty,
            Repository = parsed.Repository ?? string.Empty,
            Homepage = parsed.Homepage ?? string.Empty,
            Planners = (parsed.Planners ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
            Planters = (parsed.Planters ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
            InstalledDate = installedAt ?? File.GetLastWriteTimeUtc(planYamlPath)
        };

        return Task.FromResult<Plan?>(plan);
    }

    public override Task AddAsync(Plan entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        ValidateEntity(entity);

        var planId = GetTrimmedId(entity);
        var dir = _paths.PlanDir(planId);
        if (Directory.Exists(dir))
        {
            throw new InvalidOperationException($"Plan '{planId}' already exists.");
        }

        Directory.CreateDirectory(dir);
        var yaml = PlanYamlLite.SerializeMinimal(
            id: planId,
            version: entity.Version ?? string.Empty,
            author: entity.Author ?? string.Empty,
            license: entity.License ?? string.Empty,
            repository: entity.Repository ?? string.Empty,
            homepage: entity.Homepage ?? string.Empty,
            planners: entity.Planners ?? new List<string>(),
            planters: entity.Planters ?? new List<string>());

        File.WriteAllText(_paths.PlanYamlPath(planId), yaml, Encoding.UTF8);

        // Best-effort install metadata (source + installedAt) for compatibility with existing tooling.
        var installedAt = entity.InstalledDate == default
            ? DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            : new DateTimeOffset(DateTime.SpecifyKind(entity.InstalledDate, DateTimeKind.Utc)).ToString("O", CultureInfo.InvariantCulture);

        var metadata = new { id = planId, source = entity.Source ?? string.Empty, installedAt };
        File.WriteAllText(_paths.PlanInstallJsonPath(planId), JsonSerializer.Serialize(metadata, new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8);

        return Task.CompletedTask;
    }

    public override Task UpdateAsync(Plan entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        ValidateEntity(entity);

        var planId = GetTrimmedId(entity);
        Directory.CreateDirectory(_paths.PlanDir(planId));
        var yaml = PlanYamlLite.SerializeMinimal(
            id: planId,
            version: entity.Version ?? string.Empty,
            author: entity.Author ?? string.Empty,
            license: entity.License ?? string.Empty,
            repository: entity.Repository ?? string.Empty,
            homepage: entity.Homepage ?? string.Empty,
            planners: entity.Planners ?? new List<string>(),
            planters: entity.Planters ?? new List<string>());

        File.WriteAllText(_paths.PlanYamlPath(planId), yaml, Encoding.UTF8);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(Plan entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) return Task.CompletedTask;

        var dir = _paths.PlanDir(entity.Id.Trim());
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }

        return Task.CompletedTask;
    }

    protected override List<Plan> LoadAllPlans()
    {
        var dir = _paths.PlansDir;
        if (!Directory.Exists(dir))
        {
            return new List<Plan>();
        }

        var results = new List<Plan>();
        foreach (var planDir in Directory.GetDirectories(dir))
        {
            var planYaml = Path.Combine(planDir, "plan.yaml");
            if (!File.Exists(planYaml))
            {
                continue;
            }

            var planId = Path.GetFileName(planDir);
            var yaml = File.ReadAllText(planYaml, Encoding.UTF8);
            var parsed = PlanYamlLite.Parse(yaml);

            var installJson = Path.Combine(planDir, "install.json");
            var installedAt = TryReadInstalledAt(installJson);
            var source = TryReadPlanSource(installJson);

            results.Add(new Plan
            {
                Id = string.IsNullOrWhiteSpace(parsed.Id) ? planId : parsed.Id,
                Version = parsed.Version ?? string.Empty,
                Source = source,
                Author = parsed.Author ?? string.Empty,
                License = parsed.License ?? string.Empty,
                Repository = parsed.Repository ?? string.Empty,
                Homepage = parsed.Homepage ?? string.Empty,
                Planners = (parsed.Planners ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
                Planters = (parsed.Planters ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
                InstalledDate = installedAt ?? File.GetLastWriteTimeUtc(planYaml)
            });
        }

        return results;
    }

    private static DateTime? TryReadInstalledAt(string installJsonPath)
    {
        if (!File.Exists(installJsonPath))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(installJsonPath, Encoding.UTF8));
            if (doc.RootElement.TryGetProperty("installedAt", out var iat))
            {
                var text = iat.GetString();
                if (!string.IsNullOrWhiteSpace(text) && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
                {
                    return dto.UtcDateTime;
                }
            }
        }
        catch
        {
            // best-effort metadata
        }

        return null;
    }

    private static string TryReadPlanSource(string installJsonPath)
    {
        if (!File.Exists(installJsonPath))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(installJsonPath, Encoding.UTF8));
            if (doc.RootElement.TryGetProperty("source", out var src))
            {
                return src.GetString() ?? string.Empty;
            }
        }
        catch
        {
            // best-effort metadata
        }

        return string.Empty;
    }
}

