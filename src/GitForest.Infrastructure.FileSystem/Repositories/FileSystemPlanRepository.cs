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

    public override Task<Plan?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default
    )
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

        var yaml = FileSystemRepositoryFs.ReadAllTextUtf8(planYamlPath);
        var parsed = PlanYamlLite.Parse(yaml);

        var installJsonPath = _paths.PlanInstallJsonPath(planId);
        var installedAt = PlanFileMapper.TryReadInstalledAt(installJsonPath);
        var source = PlanFileMapper.TryReadPlanSource(installJsonPath);
        var installedDate = installedAt ?? File.GetLastWriteTimeUtc(planYamlPath);

        return Task.FromResult<Plan?>(
            PlanFileMapper.ToDomain(parsed, planId, source, installedDate)
        );
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
        var yaml = PlanFileMapper.SerializeMinimalYaml(planId, entity);
        FileSystemRepositoryFs.WriteAllTextUtf8(_paths.PlanYamlPath(planId), yaml);

        FileSystemRepositoryFs.WriteAllTextUtf8(
            _paths.PlanInstallJsonPath(planId),
            PlanFileMapper.SerializeInstallJsonForAdd(planId, entity)
        );

        return Task.CompletedTask;
    }

    public override Task UpdateAsync(Plan entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        ValidateEntity(entity);

        var planId = GetTrimmedId(entity);
        Directory.CreateDirectory(_paths.PlanDir(planId));
        var yaml = PlanFileMapper.SerializeMinimalYaml(planId, entity);
        FileSystemRepositoryFs.WriteAllTextUtf8(_paths.PlanYamlPath(planId), yaml);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(Plan entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id))
            return Task.CompletedTask;

        var dir = _paths.PlanDir(entity.Id.Trim());
        FileSystemRepositoryFs.DeleteDirectoryIfExists(dir);

        return Task.CompletedTask;
    }

    protected override List<Plan> LoadAllPlans()
    {
        return FileSystemRepositoryFs.LoadAllFromSubdirectories(
            parentDir: _paths.PlansDir,
            yamlFileName: "plan.yaml",
            loader: (planDir, planId, yaml) =>
            {
                var parsed = PlanYamlLite.Parse(yaml);

                var installJson = Path.Combine(planDir, "install.json");
                var installedAt = PlanFileMapper.TryReadInstalledAt(installJson);
                var source = PlanFileMapper.TryReadPlanSource(installJson);

                var planYamlPath = Path.Combine(planDir, "plan.yaml");
                var installedDate = installedAt ?? File.GetLastWriteTimeUtc(planYamlPath);

                return PlanFileMapper.ToDomain(parsed, planId, source, installedDate);
            }
        );
    }
}
