using System.Text.Json;
using GitForest.Core;
using GitForest.Infrastructure.FileSystem.Serialization;

namespace GitForest.Infrastructure.FileSystem.Repositories;

internal static class PlannerFileMapper
{
    public static PlannerFileModel ToFileModel(Planner planner)
    {
        if (planner is null) throw new ArgumentNullException(nameof(planner));

        var id = planner.Id.Trim();

        var config = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        if (planner.Configuration is not null)
        {
            foreach (var kv in planner.Configuration)
            {
                try
                {
                    using var doc = JsonDocument.Parse(JsonSerializer.Serialize(kv.Value));
                    config[kv.Key] = doc.RootElement.Clone();
                }
                catch
                {
                    using var doc = JsonDocument.Parse(JsonSerializer.Serialize(kv.Value?.ToString() ?? string.Empty));
                    config[kv.Key] = doc.RootElement.Clone();
                }
            }
        }

        return new PlannerFileModel(
            Id: id,
            Name: planner.Name ?? string.Empty,
            PlanId: planner.PlanId ?? string.Empty,
            Type: planner.Type ?? string.Empty,
            Configuration: config);
    }

    public static Planner ToDomain(PlannerFileModel model, string fallbackId)
    {
        var config = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in model.Configuration ?? new Dictionary<string, JsonElement>())
        {
            config[kv.Key] = kv.Value.ValueKind switch
            {
                JsonValueKind.String => kv.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => kv.Value.TryGetInt64(out var l) ? l : kv.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => kv.Value.GetRawText()
            };
        }

        return new Planner
        {
            Id = string.IsNullOrWhiteSpace(model.Id) ? fallbackId : model.Id,
            Name = model.Name ?? string.Empty,
            PlanId = model.PlanId ?? string.Empty,
            Type = model.Type ?? string.Empty,
            Configuration = config
        };
    }
}

