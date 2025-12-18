using System.Globalization;
using System.Text;

namespace GitForest.Infrastructure.FileSystem.Serialization;

public sealed record PlantFileModel(
    string Key,
    string Status,
    string Title,
    string PlanId,
    string? PlannerId,
    IReadOnlyList<string> AssignedPlanters,
    IReadOnlyList<string> Branches,
    string CreatedAt,
    string? UpdatedAt,
    string? Description);

/// <summary>
/// Minimal YAML parsing/serialization for git-forest plant.yaml.
/// This is intentionally "lite" (no YAML dependency) and supports only the shapes we need.
/// </summary>
public static class PlantYamlLite
{
    private const string DefaultStatus = "planned";

    public static string Serialize(PlantFileModel plant)
    {
        // Minimal plant.yaml aligned with docs and CLI.md (v0 contract).
        var sb = new StringBuilder();
        sb.Append("key: ").Append(plant.Key).AppendLine();
        sb.Append("status: ").Append(string.IsNullOrWhiteSpace(plant.Status) ? DefaultStatus : plant.Status).AppendLine();
        sb.Append("title: ").Append(EscapeScalar(plant.Title)).AppendLine();
        sb.Append("plan_id: ").Append(plant.PlanId).AppendLine();

        if (!string.IsNullOrWhiteSpace(plant.Description))
        {
            sb.Append("description: ").Append(EscapeScalar(plant.Description!)).AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(plant.PlannerId))
        {
            sb.AppendLine("context:");
            sb.Append("  planner: ").Append(plant.PlannerId).AppendLine();
        }

        sb.AppendLine("assigned_planters:");
        foreach (var planter in plant.AssignedPlanters ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(planter))
            {
                sb.Append("  - ").Append(planter.Trim()).AppendLine();
            }
        }

        if (plant.Branches is null || plant.Branches.Count == 0)
        {
            sb.AppendLine("branches: []");
        }
        else
        {
            sb.AppendLine("branches:");
            foreach (var br in plant.Branches)
            {
                if (!string.IsNullOrWhiteSpace(br))
                {
                    sb.Append("  - ").Append(br.Trim()).AppendLine();
                }
            }
        }

        var createdAt = string.IsNullOrWhiteSpace(plant.CreatedAt)
            ? DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            : plant.CreatedAt.Trim();
        sb.Append("created_at: ").Append(createdAt).AppendLine();
        if (!string.IsNullOrWhiteSpace(plant.UpdatedAt))
        {
            sb.Append("updated_at: ").Append(plant.UpdatedAt.Trim()).AppendLine();
        }
        return sb.ToString();
    }

    public static PlantFileModel Parse(string yaml)
    {
        var key = string.Empty;
        var status = DefaultStatus;
        var title = string.Empty;
        var description = string.Empty;
        var planId = string.Empty;
        string? plannerId = null;
        var assignedPlanters = new List<string>();
        var branches = new List<string>();
        var createdAt = string.Empty;
        string? updatedAt = null;

        var lines = (yaml ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');

        string? currentList = null;
        var inContext = false;

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            if (!char.IsWhiteSpace(line[0]))
            {
                currentList = null;
                inContext = false;

                if (TryParseScalar(line, "key", out var v)) { key = v; continue; }
                if (TryParseScalar(line, "status", out v)) { status = v; continue; }
                if (TryParseScalar(line, "title", out v)) { title = v; continue; }
                if (TryParseScalar(line, "description", out v)) { description = v; continue; }
                if (TryParseScalar(line, "plan_id", out v)) { planId = v; continue; }
                if (TryParseScalar(line, "created_at", out v)) { createdAt = v; continue; }
                if (TryParseScalar(line, "updated_at", out v)) { updatedAt = v; continue; }
                if (line.StartsWith("assigned_planters:", StringComparison.Ordinal)) { currentList = "assigned_planters"; continue; }
                if (line.StartsWith("branches:", StringComparison.Ordinal))
                {
                    // Support inline empty list: branches: []
                    if (line.TrimEnd().EndsWith("[]", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    currentList = "branches";
                    continue;
                }
                if (line.StartsWith("context:", StringComparison.Ordinal)) { inContext = true; continue; }
                continue;
            }

            if (inContext)
            {
                var trimmed = line.TrimStart();
                if (TryParseScalar(trimmed, "planner", out var p))
                {
                    plannerId = p;
                }

                continue;
            }

            if (currentList == "assigned_planters")
            {
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("- ", StringComparison.Ordinal))
                {
                    var item = trimmed[2..].Trim();
                    if (item.Length > 0)
                    {
                        assignedPlanters.Add(Unquote(item));
                    }
                }
            }

            if (currentList == "branches")
            {
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("- ", StringComparison.Ordinal))
                {
                    var item = trimmed[2..].Trim();
                    if (item.Length > 0)
                    {
                        branches.Add(Unquote(item));
                    }
                }
            }
        }

        // Best-effort fallbacks
        if (string.IsNullOrWhiteSpace(planId) && !string.IsNullOrWhiteSpace(key))
        {
            var idx = key.IndexOf(':');
            if (idx > 0)
            {
                planId = key[..idx];
            }
        }

        if (string.IsNullOrWhiteSpace(createdAt))
        {
            createdAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        }

        return new PlantFileModel(
            Key: key,
            Status: string.IsNullOrWhiteSpace(status) ? DefaultStatus : status,
            Title: title,
            PlanId: planId,
            PlannerId: plannerId,
            AssignedPlanters: assignedPlanters,
            Branches: branches,
            CreatedAt: createdAt,
            UpdatedAt: updatedAt,
            Description: string.IsNullOrWhiteSpace(description) ? null : description);
    }

    private static bool TryParseScalar(string line, string key, out string value)
    {
        value = string.Empty;
        if (!line.StartsWith($"{key}:", StringComparison.Ordinal))
        {
            return false;
        }

        value = Unquote(line[(key.Length + 1)..].Trim());
        return true;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }

    private static string EscapeScalar(string value)
    {
        var v = value ?? string.Empty;
        if (v.Contains(':') || v.Contains('#') || v.Contains('"') || v.Contains('\'') || v.Contains('\\'))
        {
            // YAML double-quoted scalar (minimal escaping)
            var escaped = v.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
            return $"\"{escaped}\"";
        }

        return v;
    }
}


