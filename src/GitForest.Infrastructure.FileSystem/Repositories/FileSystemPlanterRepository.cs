using System.Text;
using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Infrastructure.FileSystem.Serialization;

namespace GitForest.Infrastructure.FileSystem.Repositories;

public sealed class FileSystemPlanterRepository : AbstractPlanterRepository
{
    private readonly FileSystemForestPaths _paths;

    public FileSystemPlanterRepository(string forestDir)
    {
        _paths = new FileSystemForestPaths(forestDir);
    }

    public override Task<Planter?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (string.IsNullOrWhiteSpace(id)) return Task.FromResult<Planter?>(null);

        var planterId = id.Trim();
        var planterYaml = _paths.PlanterYamlPath(planterId);
        if (!File.Exists(planterYaml)) return Task.FromResult<Planter?>(null);

        var yaml = File.ReadAllText(planterYaml, Encoding.UTF8);
        var model = PlanterYamlLite.Parse(yaml);
        return Task.FromResult<Planter?>(ToDomain(model, planterId));
    }

    public override Task AddAsync(Planter entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        ValidateEntity(entity);

        var id = GetTrimmedId(entity);
        var dir = _paths.PlanterDir(id);
        if (Directory.Exists(dir))
        {
            throw new InvalidOperationException($"Planter '{id}' already exists.");
        }

        Directory.CreateDirectory(dir);
        WritePlanterYaml(entity);
        return Task.CompletedTask;
    }

    public override Task UpdateAsync(Planter entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        ValidateEntity(entity);

        Directory.CreateDirectory(_paths.PlantersDir);
        Directory.CreateDirectory(_paths.PlanterDir(GetTrimmedId(entity)));
        WritePlanterYaml(entity);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(Planter entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) return Task.CompletedTask;

        var dir = _paths.PlanterDir(entity.Id.Trim());
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }

        return Task.CompletedTask;
    }

    private void WritePlanterYaml(Planter planter)
    {
        var id = planter.Id.Trim();
        var model = new PlanterFileModel(
            Id: id,
            Name: planter.Name ?? string.Empty,
            Type: planter.Type ?? "builtin",
            Origin: planter.Origin ?? "plan",
            AssignedPlants: planter.AssignedPlants ?? new List<string>(),
            IsActive: planter.IsActive);

        File.WriteAllText(_paths.PlanterYamlPath(id), PlanterYamlLite.Serialize(model), Encoding.UTF8);
    }

    private static Planter ToDomain(PlanterFileModel model, string fallbackId)
    {
        return new Planter
        {
            Id = string.IsNullOrWhiteSpace(model.Id) ? fallbackId : model.Id,
            Name = model.Name ?? string.Empty,
            Type = string.IsNullOrWhiteSpace(model.Type) ? "builtin" : model.Type,
            Origin = string.IsNullOrWhiteSpace(model.Origin) ? "plan" : model.Origin,
            AssignedPlants = (model.AssignedPlants ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
            IsActive = model.IsActive
        };
    }

    protected override List<Planter> LoadAll()
    {
        var dir = _paths.PlantersDir;
        if (!Directory.Exists(dir))
        {
            return new List<Planter>();
        }

        var results = new List<Planter>();
        foreach (var planterDir in Directory.GetDirectories(dir))
        {
            var planterYaml = Path.Combine(planterDir, "planter.yaml");
            if (!File.Exists(planterYaml))
            {
                continue;
            }

            var id = Path.GetFileName(planterDir);
            var yaml = File.ReadAllText(planterYaml, Encoding.UTF8);
            var model = PlanterYamlLite.Parse(yaml);
            results.Add(ToDomain(model, id));
        }

        return results;
    }
}

