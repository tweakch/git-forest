using System.Text;
using GitForest.Core;
using GitForest.Core.Services;
using GitForest.Infrastructure.FileSystem.Serialization;

namespace GitForest.Infrastructure.FileSystem.Catalog;

/// <summary>
/// Reads plans from the config/plans directory (catalog) before installation.
/// </summary>
public sealed class FileSystemCatalogPlanReader : ICatalogPlanReader
{
    private readonly string _catalogPath;

    public FileSystemCatalogPlanReader(string catalogPath)
    {
        _catalogPath = catalogPath ?? throw new ArgumentNullException(nameof(catalogPath));
    }

    public Task<IReadOnlyList<CatalogPlan>> ListCatalogPlansAsync(
        CancellationToken cancellationToken = default
    )
    {
        _ = cancellationToken;

        if (!Directory.Exists(_catalogPath))
        {
            return Task.FromResult<IReadOnlyList<CatalogPlan>>(Array.Empty<CatalogPlan>());
        }

        var plans = new List<CatalogPlan>();

        // Find all .yaml files recursively in config/plans
        var yamlFiles = Directory.GetFiles(_catalogPath, "*.yaml", SearchOption.AllDirectories);

        foreach (var file in yamlFiles)
        {
            try
            {
                var plan = LoadPlanFromFile(file);
                if (plan != null)
                {
                    plans.Add(plan);
                }
            }
            catch
            {
                // Skip files that can't be parsed
            }
        }

        return Task.FromResult<IReadOnlyList<CatalogPlan>>(plans);
    }

    public Task<CatalogPlan?> GetCatalogPlanByIdAsync(
        string planId,
        CancellationToken cancellationToken = default
    )
    {
        _ = cancellationToken;

        if (string.IsNullOrWhiteSpace(planId) || !Directory.Exists(_catalogPath))
        {
            return Task.FromResult<CatalogPlan?>(null);
        }

        // Find all .yaml files recursively in config/plans
        var yamlFiles = Directory.GetFiles(_catalogPath, "*.yaml", SearchOption.AllDirectories);

        foreach (var file in yamlFiles)
        {
            try
            {
                var plan = LoadPlanFromFile(file);
                if (
                    plan != null
                    && string.Equals(plan.Id, planId.Trim(), StringComparison.OrdinalIgnoreCase)
                )
                {
                    return Task.FromResult<CatalogPlan?>(plan);
                }
            }
            catch
            {
                // Skip files that can't be parsed
            }
        }

        return Task.FromResult<CatalogPlan?>(null);
    }

    private static CatalogPlan? LoadPlanFromFile(string filePath)
    {
        var yaml = File.ReadAllText(filePath, Encoding.UTF8);
        var parsed = PlanYamlLite.Parse(yaml);

        if (string.IsNullOrWhiteSpace(parsed.Id))
        {
            return null;
        }

        return new CatalogPlan
        {
            Id = parsed.Id,
            Name = parsed.Name ?? string.Empty,
            Version = parsed.Version ?? string.Empty,
            Category = parsed.Category ?? string.Empty,
            Description = ExtractDescription(yaml),
            FilePath = filePath,
            Author = parsed.Author ?? string.Empty,
            License = parsed.License ?? string.Empty,
            Repository = parsed.Repository ?? string.Empty,
            Homepage = parsed.Homepage ?? string.Empty,
            Planners = parsed.Planners.ToList(),
            Planters = parsed.Planters.ToList(),
            Scopes = ExtractScopes(yaml),
        };
    }

    private static string ExtractDescription(string yaml)
    {
        // Extract multi-line description from YAML
        var lines = yaml.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var inDescription = false;
        var description = new StringBuilder();

        foreach (var line in lines)
        {
            if (line.StartsWith("description:", StringComparison.Ordinal))
            {
                var value = line["description:".Length..].Trim();
                if (value == "|" || value == ">")
                {
                    inDescription = true;
                    continue;
                }
                else if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim('"', '\'');
                }
            }
            else if (inDescription)
            {
                if (line.Length > 0 && !char.IsWhiteSpace(line[0]))
                {
                    // End of description block
                    break;
                }
                else if (line.TrimStart().Length > 0)
                {
                    description.AppendLine(line.TrimStart());
                }
            }
        }

        return description.ToString().Trim();
    }

    private static List<string> ExtractScopes(string yaml)
    {
        // Extract scopes list from YAML
        var lines = yaml.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var inScopes = false;
        var scopes = new List<string>();

        foreach (var line in lines)
        {
            if (line.StartsWith("scopes:", StringComparison.Ordinal))
            {
                inScopes = true;
                continue;
            }
            else if (inScopes)
            {
                if (line.Length > 0 && !char.IsWhiteSpace(line[0]))
                {
                    // End of scopes block
                    break;
                }
                else if (line.TrimStart().StartsWith("- ", StringComparison.Ordinal))
                {
                    var scope = line.TrimStart()[2..].Trim().Trim('"', '\'');
                    if (!string.IsNullOrWhiteSpace(scope))
                    {
                        scopes.Add(scope);
                    }
                }
            }
        }

        return scopes;
    }
}
