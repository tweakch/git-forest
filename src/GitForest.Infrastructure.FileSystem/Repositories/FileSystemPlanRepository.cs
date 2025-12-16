using System.Globalization;
using System.Text;
using System.Text.Json;
using Ardalis.Specification;
using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Infrastructure.FileSystem.Serialization;

namespace GitForest.Infrastructure.FileSystem.Repositories;

public sealed class FileSystemPlanRepository : IPlanRepository
{
    private readonly FileSystemForestPaths _paths;

    public FileSystemPlanRepository(string forestDir)
    {
        _paths = new FileSystemForestPaths(forestDir);
    }

    public Task<Plan?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
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

    public Task<Plan?> GetBySpecAsync(ISpecification<Plan> specification, CancellationToken cancellationToken = default)
    {
        return GetBySpecInternalAsync(specification, cancellationToken);
    }

    public Task<TResult?> GetBySpecAsync<TResult>(ISpecification<Plan, TResult> specification, CancellationToken cancellationToken = default)
    {
        return GetBySpecInternalAsync(specification, cancellationToken);
    }

    public Task<IReadOnlyList<Plan>> ListAsync(ISpecification<Plan> specification, CancellationToken cancellationToken = default)
    {
        return ListBySpecInternalAsync(specification, cancellationToken);
    }

    public Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<Plan, TResult> specification, CancellationToken cancellationToken = default)
    {
        return ListBySpecInternalAsync(specification, cancellationToken);
    }

    public Task AddAsync(Plan entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Plan.Id must be provided.", nameof(entity));

        var planId = entity.Id.Trim();
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

    public Task UpdateAsync(Plan entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Plan.Id must be provided.", nameof(entity));

        var planId = entity.Id.Trim();
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

    public Task DeleteAsync(Plan entity, CancellationToken cancellationToken = default)
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

    private Task<T?> GetBySpecInternalAsync<T>(ISpecification<Plan, T> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAllPlans();
        return Task.FromResult(InMemorySpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<Plan?> GetBySpecInternalAsync(ISpecification<Plan> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAllPlans();
        return Task.FromResult(InMemorySpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<IReadOnlyList<T>> ListBySpecInternalAsync<T>(ISpecification<Plan, T> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAllPlans();
        return Task.FromResult((IReadOnlyList<T>)InMemorySpecificationEvaluator.Apply(all, specification).ToList());
    }

    private Task<IReadOnlyList<Plan>> ListBySpecInternalAsync(ISpecification<Plan> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAllPlans();
        return Task.FromResult((IReadOnlyList<Plan>)InMemorySpecificationEvaluator.Apply(all, specification).ToList());
    }

    private List<Plan> LoadAllPlans()
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

