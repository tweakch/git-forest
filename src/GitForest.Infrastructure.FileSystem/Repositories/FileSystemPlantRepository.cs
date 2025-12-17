using System.Globalization;
using System.Text;
using Ardalis.Specification;
using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Infrastructure.FileSystem.Serialization;

namespace GitForest.Infrastructure.FileSystem.Repositories;

public sealed class FileSystemPlantRepository : IPlantRepository
{
    private readonly FileSystemForestPaths _paths;

    public FileSystemPlantRepository(string forestDir)
    {
        _paths = new FileSystemForestPaths(forestDir);
    }

    public Task<Plant?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (string.IsNullOrWhiteSpace(id))
        {
            return Task.FromResult<Plant?>(null);
        }

        var key = id.Trim();
        var plantYamlPath = _paths.PlantYamlPathFromKey(key);
        if (!File.Exists(plantYamlPath))
        {
            return Task.FromResult<Plant?>(null);
        }

        var yaml = File.ReadAllText(plantYamlPath, Encoding.UTF8);
        var model = PlantYamlLite.Parse(yaml);
        return Task.FromResult<Plant?>(ToDomain(model));
    }

    public Task<Plant?> GetBySpecAsync(ISpecification<Plant> specification, CancellationToken cancellationToken = default)
    {
        return GetBySpecInternalAsync(specification, cancellationToken);
    }

    public Task<TResult?> GetBySpecAsync<TResult>(ISpecification<Plant, TResult> specification, CancellationToken cancellationToken = default)
    {
        return GetBySpecInternalAsync(specification, cancellationToken);
    }

    public Task<IReadOnlyList<Plant>> ListAsync(ISpecification<Plant> specification, CancellationToken cancellationToken = default)
    {
        return ListBySpecInternalAsync(specification, cancellationToken);
    }

    public Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<Plant, TResult> specification, CancellationToken cancellationToken = default)
    {
        return ListBySpecInternalAsync(specification, cancellationToken);
    }

    public Task AddAsync(Plant entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Key)) throw new ArgumentException("Plant.Key must be provided.", nameof(entity));

        var plantKey = entity.Key.Trim();
        var dir = _paths.PlantDirFromKey(plantKey);
        if (Directory.Exists(dir))
        {
            throw new InvalidOperationException($"Plant '{plantKey}' already exists.");
        }

        Directory.CreateDirectory(dir);
        WritePlantYaml(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Plant entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Key)) throw new ArgumentException("Plant.Key must be provided.", nameof(entity));

        Directory.CreateDirectory(_paths.PlantsDir);
        Directory.CreateDirectory(_paths.PlantDirFromKey(entity.Key.Trim()));
        WritePlantYaml(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Plant entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Key)) return Task.CompletedTask;

        var dir = _paths.PlantDirFromKey(entity.Key.Trim());
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }

        return Task.CompletedTask;
    }

    private void WritePlantYaml(Plant plant)
    {
        var key = plant.Key.Trim();
        var (planId, slug) = FileSystemForestPaths.SplitPlantKey(key);
        var created = plant.CreatedDate == default ? DateTimeOffset.UtcNow : new DateTimeOffset(DateTime.SpecifyKind(plant.CreatedDate, DateTimeKind.Utc));
        var updated = plant.LastActivityDate.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(plant.LastActivityDate.Value, DateTimeKind.Utc)) : (DateTimeOffset?)null;

        var model = new PlantFileModel(
            Key: key,
            Status: plant.Status ?? "planned",
            Title: plant.Title ?? string.Empty,
            PlanId: string.IsNullOrWhiteSpace(plant.PlanId) ? planId : plant.PlanId,
            PlannerId: string.IsNullOrWhiteSpace(plant.PlannerId) ? null : plant.PlannerId,
            AssignedPlanters: plant.AssignedPlanters ?? new List<string>(),
            Branches: plant.Branches ?? new List<string>(),
            CreatedAt: created.ToString("O", CultureInfo.InvariantCulture),
            UpdatedAt: updated?.ToString("O", CultureInfo.InvariantCulture),
            Description: string.IsNullOrWhiteSpace(plant.Description) ? null : plant.Description);

        var plantYamlPath = _paths.PlantYamlPathFromKey(key);
        File.WriteAllText(plantYamlPath, PlantYamlLite.Serialize(model), Encoding.UTF8);

        // Keep slug in sync (in-memory only)
        plant.Slug = string.IsNullOrWhiteSpace(plant.Slug) ? slug : plant.Slug;
        plant.PlanId = string.IsNullOrWhiteSpace(plant.PlanId) ? planId : plant.PlanId;
    }

    private static Plant ToDomain(PlantFileModel model)
    {
        var (planId, slug) = FileSystemForestPaths.SplitPlantKey(model.Key);
        var created = TryParseRoundtripUtc(model.CreatedAt) ?? DateTime.UtcNow;
        var updated = TryParseRoundtripUtc(model.UpdatedAt);
        return new Plant
        {
            Key = model.Key ?? string.Empty,
            Slug = slug,
            PlanId = string.IsNullOrWhiteSpace(model.PlanId) ? planId : model.PlanId,
            PlannerId = model.PlannerId ?? string.Empty,
            Status = string.IsNullOrWhiteSpace(model.Status) ? "planned" : model.Status,
            Title = model.Title ?? string.Empty,
            Description = model.Description ?? string.Empty,
            AssignedPlanters = (model.AssignedPlanters ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
            Branches = (model.Branches ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
            CreatedDate = created,
            LastActivityDate = updated
        };
    }

    private static DateTime? TryParseRoundtripUtc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
        {
            return dto.UtcDateTime;
        }

        return null;
    }

    private Task<T?> GetBySpecInternalAsync<T>(ISpecification<Plant, T> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAllPlants();
        return Task.FromResult(GitForest.Core.Persistence.SpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<Plant?> GetBySpecInternalAsync(ISpecification<Plant> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAllPlants();
        return Task.FromResult(GitForest.Core.Persistence.SpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<IReadOnlyList<T>> ListBySpecInternalAsync<T>(ISpecification<Plant, T> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAllPlants();
        return Task.FromResult((IReadOnlyList<T>)GitForest.Core.Persistence.SpecificationEvaluator.Apply(all, specification).ToList());
    }

    private Task<IReadOnlyList<Plant>> ListBySpecInternalAsync(ISpecification<Plant> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAllPlants();
        return Task.FromResult((IReadOnlyList<Plant>)GitForest.Core.Persistence.SpecificationEvaluator.Apply(all, specification).ToList());
    }

    private List<Plant> LoadAllPlants()
    {
        var dir = _paths.PlantsDir;
        if (!Directory.Exists(dir))
        {
            return new List<Plant>();
        }

        var results = new List<Plant>();
        foreach (var plantDir in Directory.GetDirectories(dir))
        {
            var plantYaml = Path.Combine(plantDir, "plant.yaml");
            if (!File.Exists(plantYaml))
            {
                continue;
            }

            var yaml = File.ReadAllText(plantYaml, Encoding.UTF8);
            var model = PlantYamlLite.Parse(yaml);
            results.Add(ToDomain(model));
        }

        return results;
    }
}

