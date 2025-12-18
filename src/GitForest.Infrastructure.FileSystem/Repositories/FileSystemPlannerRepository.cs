using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Infrastructure.FileSystem.Serialization;

namespace GitForest.Infrastructure.FileSystem.Repositories;

public sealed class FileSystemPlannerRepository : AbstractPlannerRepository
{
    private readonly FileSystemForestPaths _paths;

    public FileSystemPlannerRepository(string forestDir)
    {
        _paths = new FileSystemForestPaths(forestDir);
    }

    public override Task<Planner?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (string.IsNullOrWhiteSpace(id)) return Task.FromResult<Planner?>(null);

        var plannerId = id.Trim();
        var plannerYaml = _paths.PlannerYamlPath(plannerId);
        if (!File.Exists(plannerYaml)) return Task.FromResult<Planner?>(null);

        var yaml = FileSystemRepositoryFs.ReadAllTextUtf8(plannerYaml);
        var model = PlannerYamlLite.Parse(yaml);
        return Task.FromResult<Planner?>(PlannerFileMapper.ToDomain(model, plannerId));
    }

    public override Task AddAsync(Planner entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        ValidateEntity(entity);

        var id = GetTrimmedId(entity);
        var dir = _paths.PlannerDir(id);
        if (Directory.Exists(dir))
        {
            throw new InvalidOperationException($"Planner '{id}' already exists.");
        }

        Directory.CreateDirectory(dir);
        var model = PlannerFileMapper.ToFileModel(entity);
        FileSystemRepositoryFs.WriteAllTextUtf8(_paths.PlannerYamlPath(model.Id), PlannerYamlLite.Serialize(model));
        return Task.CompletedTask;
    }

    public override Task UpdateAsync(Planner entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        ValidateEntity(entity);

        Directory.CreateDirectory(_paths.PlannersDir);
        Directory.CreateDirectory(_paths.PlannerDir(GetTrimmedId(entity)));
        var model = PlannerFileMapper.ToFileModel(entity);
        FileSystemRepositoryFs.WriteAllTextUtf8(_paths.PlannerYamlPath(model.Id), PlannerYamlLite.Serialize(model));
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(Planner entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) return Task.CompletedTask;

        var dir = _paths.PlannerDir(entity.Id.Trim());
        FileSystemRepositoryFs.DeleteDirectoryIfExists(dir);

        return Task.CompletedTask;
    }

    protected override List<Planner> LoadAll()
    {
        return FileSystemRepositoryFs.LoadAllFromSubdirectories(
            parentDir: _paths.PlannersDir,
            yamlFileName: "planner.yaml",
            loader: (_, plannerId, yaml) =>
            {
                var model = PlannerYamlLite.Parse(yaml);
                return PlannerFileMapper.ToDomain(model, plannerId);
            });
    }
}

