using System.Text;
using System.Text.Json;
using Ardalis.Specification;
using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Infrastructure.FileSystem.Serialization;

namespace GitForest.Infrastructure.FileSystem.Repositories;

public sealed class FileSystemPlannerRepository : IPlannerRepository
{
    private readonly FileSystemForestPaths _paths;

    public FileSystemPlannerRepository(string forestDir)
    {
        _paths = new FileSystemForestPaths(forestDir);
    }

    public Task<Planner?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (string.IsNullOrWhiteSpace(id)) return Task.FromResult<Planner?>(null);

        var plannerId = id.Trim();
        var plannerYaml = _paths.PlannerYamlPath(plannerId);
        if (!File.Exists(plannerYaml)) return Task.FromResult<Planner?>(null);

        var yaml = File.ReadAllText(plannerYaml, Encoding.UTF8);
        var model = PlannerYamlLite.Parse(yaml);
        return Task.FromResult<Planner?>(ToDomain(model, plannerId));
    }

    public Task<Planner?> GetBySpecAsync(ISpecification<Planner> specification, CancellationToken cancellationToken = default)
    {
        return GetBySpecInternalAsync(specification, cancellationToken);
    }

    public Task<TResult?> GetBySpecAsync<TResult>(ISpecification<Planner, TResult> specification, CancellationToken cancellationToken = default)
    {
        return GetBySpecInternalAsync(specification, cancellationToken);
    }

    public Task<IReadOnlyList<Planner>> ListAsync(ISpecification<Planner> specification, CancellationToken cancellationToken = default)
    {
        return ListBySpecInternalAsync(specification, cancellationToken);
    }

    public Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<Planner, TResult> specification, CancellationToken cancellationToken = default)
    {
        return ListBySpecInternalAsync(specification, cancellationToken);
    }

    public Task AddAsync(Planner entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Planner.Id must be provided.", nameof(entity));

        var id = entity.Id.Trim();
        var dir = _paths.PlannerDir(id);
        if (Directory.Exists(dir))
        {
            throw new InvalidOperationException($"Planner '{id}' already exists.");
        }

        Directory.CreateDirectory(dir);
        WritePlannerYaml(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Planner entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Planner.Id must be provided.", nameof(entity));

        Directory.CreateDirectory(_paths.PlannersDir);
        Directory.CreateDirectory(_paths.PlannerDir(entity.Id.Trim()));
        WritePlannerYaml(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Planner entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) return Task.CompletedTask;

        var dir = _paths.PlannerDir(entity.Id.Trim());
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }

        return Task.CompletedTask;
    }

    private void WritePlannerYaml(Planner planner)
    {
        var id = planner.Id.Trim();
        var config = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        if (planner.Configuration is not null)
        {
            foreach (var kv in planner.Configuration)
            {
                try
                {
                    using var doc = JsonDocument.Parse(JsonSerializer.Serialize(kv.Value));
                    config[kv.Key] = doc.RootElement.Clone();
                }
                catch
                {
                    using var doc = JsonDocument.Parse(JsonSerializer.Serialize(kv.Value?.ToString() ?? string.Empty));
                    config[kv.Key] = doc.RootElement.Clone();
                }
            }
        }

        var model = new PlannerFileModel(
            Id: id,
            Name: planner.Name ?? string.Empty,
            PlanId: planner.PlanId ?? string.Empty,
            Type: planner.Type ?? string.Empty,
            Configuration: config);

        File.WriteAllText(_paths.PlannerYamlPath(id), PlannerYamlLite.Serialize(model), Encoding.UTF8);
    }

    private static Planner ToDomain(PlannerFileModel model, string fallbackId)
    {
        var config = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in model.Configuration ?? new Dictionary<string, JsonElement>())
        {
            config[kv.Key] = kv.Value.ValueKind switch
            {
                JsonValueKind.String => kv.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => kv.Value.TryGetInt64(out var l) ? l : kv.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => kv.Value.GetRawText()
            };
        }

        return new Planner
        {
            Id = string.IsNullOrWhiteSpace(model.Id) ? fallbackId : model.Id,
            Name = model.Name ?? string.Empty,
            PlanId = model.PlanId ?? string.Empty,
            Type = model.Type ?? string.Empty,
            Configuration = config
        };
    }

    private Task<T?> GetBySpecInternalAsync<T>(ISpecification<Planner, T> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));
        var all = LoadAll();
        return Task.FromResult(InMemorySpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<Planner?> GetBySpecInternalAsync(ISpecification<Planner> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));
        var all = LoadAll();
        return Task.FromResult(InMemorySpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<IReadOnlyList<T>> ListBySpecInternalAsync<T>(ISpecification<Planner, T> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));
        var all = LoadAll();
        return Task.FromResult((IReadOnlyList<T>)InMemorySpecificationEvaluator.Apply(all, specification).ToList());
    }

    private Task<IReadOnlyList<Planner>> ListBySpecInternalAsync(ISpecification<Planner> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));
        var all = LoadAll();
        return Task.FromResult((IReadOnlyList<Planner>)InMemorySpecificationEvaluator.Apply(all, specification).ToList());
    }

    private List<Planner> LoadAll()
    {
        var dir = _paths.PlannersDir;
        if (!Directory.Exists(dir))
        {
            return new List<Planner>();
        }

        var results = new List<Planner>();
        foreach (var plannerDir in Directory.GetDirectories(dir))
        {
            var plannerYaml = Path.Combine(plannerDir, "planner.yaml");
            if (!File.Exists(plannerYaml))
            {
                continue;
            }

            var id = Path.GetFileName(plannerDir);
            var yaml = File.ReadAllText(plannerYaml, Encoding.UTF8);
            var model = PlannerYamlLite.Parse(yaml);
            results.Add(ToDomain(model, id));
        }

        return results;
    }
}

