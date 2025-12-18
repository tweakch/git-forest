using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Infrastructure.FileSystem.Serialization;

namespace GitForest.Infrastructure.FileSystem.Repositories;

public sealed class FileSystemPlantRepository : AbstractPlantRepository
{
    private readonly FileSystemForestPaths _paths;

    public FileSystemPlantRepository(string forestDir)
    {
        _paths = new FileSystemForestPaths(forestDir);
    }

    public override Task<Plant?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default
    )
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

        var yaml = FileSystemRepositoryFs.ReadAllTextUtf8(plantYamlPath);
        var model = PlantYamlLite.Parse(yaml);
        return Task.FromResult<Plant?>(PlantFileMapper.ToDomain(model));
    }

    public override Task AddAsync(Plant entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        ValidateEntity(entity);

        var plantKey = GetTrimmedId(entity);
        var dir = _paths.PlantDirFromKey(plantKey);
        if (Directory.Exists(dir))
        {
            throw new InvalidOperationException($"Plant '{plantKey}' already exists.");
        }

        Directory.CreateDirectory(dir);
        var model = PlantFileMapper.ToFileModelAndSyncDomain(entity);
        var plantYamlPath = _paths.PlantYamlPathFromKey(model.Key);
        FileSystemRepositoryFs.WriteAllTextUtf8(plantYamlPath, PlantYamlLite.Serialize(model));
        return Task.CompletedTask;
    }

    public override Task UpdateAsync(Plant entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        ValidateEntity(entity);

        Directory.CreateDirectory(_paths.PlantsDir);
        Directory.CreateDirectory(_paths.PlantDirFromKey(GetTrimmedId(entity)));
        var model = PlantFileMapper.ToFileModelAndSyncDomain(entity);
        var plantYamlPath = _paths.PlantYamlPathFromKey(model.Key);
        FileSystemRepositoryFs.WriteAllTextUtf8(plantYamlPath, PlantYamlLite.Serialize(model));
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(Plant entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Key))
            return Task.CompletedTask;

        var dir = _paths.PlantDirFromKey(entity.Key.Trim());
        FileSystemRepositoryFs.DeleteDirectoryIfExists(dir);

        return Task.CompletedTask;
    }

    protected override List<Plant> LoadAll()
    {
        return FileSystemRepositoryFs.LoadAllFromSubdirectories(
            parentDir: _paths.PlantsDir,
            yamlFileName: "plant.yaml",
            loader: (_, _, yaml) =>
            {
                var model = PlantYamlLite.Parse(yaml);
                return PlantFileMapper.ToDomain(model);
            }
        );
    }
}
