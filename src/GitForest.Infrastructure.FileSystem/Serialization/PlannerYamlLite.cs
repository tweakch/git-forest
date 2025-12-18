using System.Text;
using System.Text.Json;

namespace GitForest.Infrastructure.FileSystem.Serialization;

internal sealed record PlannerFileModel(
    string Id,
    string Name,
    string PlanId,
    string Type,
    IReadOnlyDictionary<string, JsonElement> Configuration);

internal static class PlannerYamlLite
{
    public static string Serialize(PlannerFileModel planner)
    {
        var sb = new StringBuilder();
        sb.Append("id: ").Append(planner.Id ?? string.Empty).AppendLine();
        if (!string.IsNullOrWhiteSpace(planner.Name)) sb.Append("name: ").Append(EscapeScalar(planner.Name.Trim())).AppendLine();
        if (!string.IsNullOrWhiteSpace(planner.PlanId)) sb.Append("plan_id: ").Append(planner.PlanId.Trim()).AppendLine();
        if (!string.IsNullOrWhiteSpace(planner.Type)) sb.Append("type: ").Append(planner.Type.Trim()).AppendLine();

        if (planner.Configuration is not null && planner.Configuration.Count > 0)
        {
            sb.AppendLine("configuration:");
            foreach (var kv in planner.Configuration.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                // Store as a JSON scalar to keep it round-trippable without needing YAML parsing.
                sb.Append("  ").Append(kv.Key).Append(": ").Append(EscapeScalar(kv.Value.GetRawText())).AppendLine();
            }
        }

        return sb.ToString();
    }

    public static PlannerFileModel Parse(string yaml)
    {
        var id = string.Empty;
        var name = string.Empty;
        var planId = string.Empty;
        var type = string.Empty;
        var config = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        var lines = (yaml ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        var inConfig = false;

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            if (!char.IsWhiteSpace(line[0]))
            {
                inConfig = false;
                if (TryParseScalar(line, "id", out var v)) { id = v; continue; }
                if (TryParseScalar(line, "name", out v)) { name = v; continue; }
                if (TryParseScalar(line, "plan_id", out v)) { planId = v; continue; }
                if (TryParseScalar(line, "type", out v)) { type = v; continue; }
                if (line.StartsWith("configuration:", StringComparison.Ordinal)) { inConfig = true; continue; }
                continue;
            }

            if (!inConfig)
            {
                continue;
            }

            // Expect: "  key: <json>"
            var trimmed = line.TrimStart();
            var idx = trimmed.IndexOf(':', StringComparison.Ordinal);
            if (idx <= 0 || idx == trimmed.Length - 1)
            {
                continue;
            }

            var key = trimmed[..idx].Trim();
            var rawValue = trimmed[(idx + 1)..].Trim();
            if (key.Length == 0 || rawValue.Length == 0)
            {
                continue;
            }

            rawValue = Unquote(rawValue);
            try
            {
                using var doc = JsonDocument.Parse(rawValue);
                config[key] = doc.RootElement.Clone();
            }
            catch
            {
                // Best-effort: store as JSON string
                using var doc = JsonDocument.Parse(JsonSerializer.Serialize(rawValue));
                config[key] = doc.RootElement.Clone();
            }
        }

        return new PlannerFileModel(
            Id: id,
            Name: name,
            PlanId: planId,
            Type: type,
            Configuration: config);
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
            var escaped = v.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
            return $"\"{escaped}\"";
        }

        return v;
    }
}


