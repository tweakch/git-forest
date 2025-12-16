using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GitForest.Core.Services;
using GitForest.Infrastructure.FileSystem.Serialization;

namespace GitForest.Infrastructure.FileSystem.Plans;

public sealed class FileSystemPlanInstaller : IPlanInstaller
{
    private readonly string _forestDir;

    public FileSystemPlanInstaller(string forestDir)
    {
        _forestDir = forestDir ?? string.Empty;
    }

    public Task<(string planId, string version)> InstallAsync(string source, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
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
            throw new FileNotFoundException("Plan file not found.", resolvedSource);
        }

        var yaml = File.ReadAllText(resolvedSource, Encoding.UTF8);
        var plan = PlanYamlLite.Parse(yaml);

        if (string.IsNullOrWhiteSpace(plan.Id))
        {
            throw new InvalidDataException($"Plan YAML at '{resolvedSource}' is missing required top-level 'id'.");
        }

        var planDir = Path.Combine(_forestDir.Trim(), "plans", plan.Id);
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
            source,
            installedAt,
            sha256
        };
        File.WriteAllText(installMetadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8);

        return Task.FromResult((planId: plan.Id, version: plan.Version ?? string.Empty));
    }

    private static string ComputeSha256Hex(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }
}

