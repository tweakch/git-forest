using System.Text;

namespace GitForest.Infrastructure.FileSystem.Serialization;

internal sealed record PlanterFileModel(
    string Id,
    string Name,
    string Type,
    string Origin,
    IReadOnlyList<string> AssignedPlants,
    bool IsActive
);

internal static class PlanterYamlLite
{
    public static string Serialize(PlanterFileModel planter)
    {
        var sb = new StringBuilder();
        sb.Append("id: ").Append(planter.Id ?? string.Empty).AppendLine();
        if (!string.IsNullOrWhiteSpace(planter.Name))
            sb.Append("name: ").Append(EscapeScalar(planter.Name.Trim())).AppendLine();
        if (!string.IsNullOrWhiteSpace(planter.Type))
            sb.Append("type: ").Append(planter.Type.Trim()).AppendLine();
        if (!string.IsNullOrWhiteSpace(planter.Origin))
            sb.Append("origin: ").Append(planter.Origin.Trim()).AppendLine();
        sb.Append("is_active: ").Append(planter.IsActive ? "true" : "false").AppendLine();

        sb.AppendLine("assigned_plants:");
        foreach (var plant in planter.AssignedPlants ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(plant))
            {
                sb.Append("  - ").Append(plant.Trim()).AppendLine();
            }
        }

        return sb.ToString();
    }

    public static PlanterFileModel Parse(string yaml)
    {
        var id = string.Empty;
        var name = string.Empty;
        var type = "builtin";
        var origin = "plan";
        var isActive = false;
        var assigned = new List<string>();

        var lines = (yaml ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
        var inAssigned = false;

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            if (!char.IsWhiteSpace(line[0]))
            {
                inAssigned = false;
                if (TryParseScalar(line, "id", out var v))
                {
                    id = v;
                    continue;
                }
                if (TryParseScalar(line, "name", out v))
                {
                    name = v;
                    continue;
                }
                if (TryParseScalar(line, "type", out v))
                {
                    type = v;
                    continue;
                }
                if (TryParseScalar(line, "origin", out v))
                {
                    origin = v;
                    continue;
                }
                if (TryParseScalar(line, "is_active", out v))
                {
                    isActive = v.Equals("true", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (line.StartsWith("assigned_plants:", StringComparison.Ordinal))
                {
                    inAssigned = true;
                    continue;
                }
                continue;
            }

            if (inAssigned)
            {
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("- ", StringComparison.Ordinal))
                {
                    var item = trimmed[2..].Trim();
                    if (item.Length > 0)
                    {
                        assigned.Add(Unquote(item));
                    }
                }
            }
        }

        return new PlanterFileModel(
            Id: id,
            Name: name,
            Type: string.IsNullOrWhiteSpace(type) ? "builtin" : type,
            Origin: string.IsNullOrWhiteSpace(origin) ? "plan" : origin,
            AssignedPlants: assigned,
            IsActive: isActive
        );
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
        if (
            value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\''))
        )
        {
            return value[1..^1];
        }

        return value;
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
            var escaped = v.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal);
            return $"\"{escaped}\"";
        }

        return v;
    }
}
