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

        var yaml = FileSystemRepositoryFs.ReadAllTextUtf8(planterYaml);
        var model = PlanterYamlLite.Parse(yaml);
        return Task.FromResult<Planter?>(PlanterFileMapper.ToDomain(model, planterId));
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
        var model = PlanterFileMapper.ToFileModel(entity);
        FileSystemRepositoryFs.WriteAllTextUtf8(_paths.PlanterYamlPath(model.Id), PlanterYamlLite.Serialize(model));
        return Task.CompletedTask;
    }

    public override Task UpdateAsync(Planter entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        ValidateEntity(entity);

        Directory.CreateDirectory(_paths.PlantersDir);
        Directory.CreateDirectory(_paths.PlanterDir(GetTrimmedId(entity)));
        var model = PlanterFileMapper.ToFileModel(entity);
        FileSystemRepositoryFs.WriteAllTextUtf8(_paths.PlanterYamlPath(model.Id), PlanterYamlLite.Serialize(model));
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(Planter entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) return Task.CompletedTask;

        var dir = _paths.PlanterDir(entity.Id.Trim());
        FileSystemRepositoryFs.DeleteDirectoryIfExists(dir);

        return Task.CompletedTask;
    }

    protected override List<Planter> LoadAll()
    {
        return FileSystemRepositoryFs.LoadAllFromSubdirectories(
            parentDir: _paths.PlantersDir,
            yamlFileName: "planter.yaml",
            loader: (_, planterId, yaml) =>
            {
                var model = PlanterYamlLite.Parse(yaml);
                return PlanterFileMapper.ToDomain(model, planterId);
            });
    }
}

