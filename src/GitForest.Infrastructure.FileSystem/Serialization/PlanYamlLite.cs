using System.Text;

namespace GitForest.Infrastructure.FileSystem.Serialization;

/// <summary>
/// Minimal YAML parsing/serialization for git-forest plan.yaml.
/// This is intentionally "lite" (no YAML dependency) and supports only the shapes we need.
/// </summary>
public static class PlanYamlLite
{
    public sealed record ParsedPlan(
        string Id,
        string Name,
        string Version,
        string Category,
        string Author,
        string License,
        string Repository,
        string Homepage,
        IReadOnlyList<string> Planners,
        IReadOnlyList<string> Planters,
        IReadOnlyList<string> PlantTemplateNames
    );

    public static ParsedPlan Parse(string yaml)
    {
        var id = string.Empty;
        var name = string.Empty;
        var version = string.Empty;
        var category = string.Empty;
        var author = string.Empty;
        var license = string.Empty;
        var repository = string.Empty;
        var homepage = string.Empty;
        var planners = new List<string>();
        var planters = new List<string>();
        var templateNames = new List<string>();

        var lines = SplitLines(yaml);

        string? currentList = null;
        var inPlantTemplates = false;

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            if (!char.IsWhiteSpace(line[0]))
            {
                inPlantTemplates = false;
                currentList = null;

                if (TryParseTopLevelScalar(line, "id", out var v))
                {
                    id = v;
                    continue;
                }
                if (TryParseTopLevelScalar(line, "name", out v))
                {
                    name = v;
                    continue;
                }
                if (TryParseTopLevelScalar(line, "version", out v))
                {
                    version = v;
                    continue;
                }
                if (TryParseTopLevelScalar(line, "category", out v))
                {
                    category = v;
                    continue;
                }
                if (TryParseTopLevelScalar(line, "author", out v))
                {
                    author = v;
                    continue;
                }
                if (TryParseTopLevelScalar(line, "license", out v))
                {
                    license = v;
                    continue;
                }
                if (TryParseTopLevelScalar(line, "repository", out v))
                {
                    repository = v;
                    continue;
                }
                if (TryParseTopLevelScalar(line, "homepage", out v))
                {
                    homepage = v;
                    continue;
                }

                if (IsTopLevelKey(line, "planners"))
                {
                    currentList = "planners";
                    continue;
                }
                if (IsTopLevelKey(line, "planters"))
                {
                    currentList = "planters";
                    continue;
                }
                if (IsTopLevelKey(line, "plant_templates"))
                {
                    inPlantTemplates = true;
                    continue;
                }

                continue;
            }

            if (currentList is not null)
            {
                if (TryParseListItem(line, out var item))
                {
                    if (currentList == "planners")
                        planners.Add(item);
                    if (currentList == "planters")
                        planters.Add(item);
                }

                continue;
            }

            if (inPlantTemplates)
            {
                // We only need the template names for deterministic seed plants.
                // Supports shapes like:
                //   - name: add-integration-tests
                //     title_template: ...
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("- ", StringComparison.Ordinal))
                {
                    trimmed = trimmed[2..].TrimStart();
                }

                if (
                    TryParseTopLevelScalar(trimmed, "name", out var tname)
                    && !string.IsNullOrWhiteSpace(tname)
                )
                {
                    templateNames.Add(tname);
                }
            }
        }

        return new ParsedPlan(
            Id: id,
            Name: name,
            Version: version,
            Category: category,
            Author: author,
            License: license,
            Repository: repository,
            Homepage: homepage,
            Planners: planners,
            Planters: planters,
            PlantTemplateNames: templateNames
        );
    }

    public static string SerializeMinimal(
        string id,
        string version,
        string author,
        string license,
        string repository,
        string homepage,
        IReadOnlyList<string> planners,
        IReadOnlyList<string> planters
    )
    {
        var sb = new StringBuilder();
        sb.Append("id: ").Append(id ?? string.Empty).AppendLine();
        if (!string.IsNullOrWhiteSpace(version))
            sb.Append("version: ").Append(version.Trim()).AppendLine();
        if (!string.IsNullOrWhiteSpace(author))
            sb.Append("author: ").Append(EscapeScalar(author.Trim())).AppendLine();
        if (!string.IsNullOrWhiteSpace(license))
            sb.Append("license: ").Append(EscapeScalar(license.Trim())).AppendLine();
        if (!string.IsNullOrWhiteSpace(repository))
            sb.Append("repository: ").Append(EscapeScalar(repository.Trim())).AppendLine();
        if (!string.IsNullOrWhiteSpace(homepage))
            sb.Append("homepage: ").Append(EscapeScalar(homepage.Trim())).AppendLine();

        sb.AppendLine("planners:");
        foreach (var p in planners ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(p))
            {
                sb.Append("  - ").Append(p.Trim()).AppendLine();
            }
        }

        sb.AppendLine("planters:");
        foreach (var p in planters ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(p))
            {
                sb.Append("  - ").Append(p.Trim()).AppendLine();
            }
        }

        return sb.ToString();
    }

    private static bool IsTopLevelKey(string line, string key)
    {
        return line.StartsWith($"{key}:", StringComparison.Ordinal);
    }

    private static bool TryParseTopLevelScalar(string line, string key, out string value)
    {
        value = string.Empty;
        if (!line.StartsWith($"{key}:", StringComparison.Ordinal))
        {
            return false;
        }

        value = line[(key.Length + 1)..].Trim();
        // Drop wrapping quotes if present.
        if (
            value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\''))
        )
        {
            value = value[1..^1];
        }

        return true;
    }

    private static bool TryParseListItem(string line, out string value)
    {
        value = string.Empty;
        var trimmed = line.TrimStart();
        if (!trimmed.StartsWith("- ", StringComparison.Ordinal))
        {
            return false;
        }

        value = trimmed[2..].Trim();
        if (
            value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\''))
        )
        {
            value = value[1..^1];
        }

        return value.Length > 0;
    }

    private static string[] SplitLines(string yaml)
    {
        return (yaml ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
    }

    private static string EscapeScalar(string value)
    {
        var v = value ?? string.Empty;
        if (
            v.Contains(':')
            || v.Contains('#')
            || v.Contains('"')
            || v.Contains('\'')
            || v.Contains('\\')
        )
        {
            // YAML double-quoted scalar (minimal escaping)
            var escaped = v.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal);
            return $"\"{escaped}\"";
        }

        return v;
    }
}

