using System.Globalization;
using GitForest.Core;
using GitForest.Infrastructure.FileSystem.Serialization;

namespace GitForest.Infrastructure.FileSystem.Repositories;

internal static class PlantFileMapper
{
    public static PlantFileModel ToFileModelAndSyncDomain(Plant plant)
    {
        if (plant is null) throw new ArgumentNullException(nameof(plant));

        var key = plant.Key.Trim();
        var (planId, slug) = FileSystemForestPaths.SplitPlantKey(key);
        var created = plant.CreatedDate == default
            ? DateTimeOffset.UtcNow
            : new DateTimeOffset(DateTime.SpecifyKind(plant.CreatedDate, DateTimeKind.Utc));
        var updated = plant.LastActivityDate.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(plant.LastActivityDate.Value, DateTimeKind.Utc))
            : (DateTimeOffset?)null;

        var model = new PlantFileModel(
            Key: key,
            Status: plant.Status ?? "planned",
            Title: plant.Title ?? string.Empty,
            PlanId: string.IsNullOrWhiteSpace(plant.PlanId) ? planId : plant.PlanId,
            PlannerId: string.IsNullOrWhiteSpace(plant.PlannerId) ? null : plant.PlannerId,
            AssignedPlanters: plant.AssignedPlanters ?? new List<string>(),
            Branches: plant.Branches ?? new List<string>(),
            CreatedAt: created.ToString("O", CultureInfo.InvariantCulture),
            UpdatedAt: updated?.ToString("O", CultureInfo.InvariantCulture),
            Description: string.IsNullOrWhiteSpace(plant.Description) ? null : plant.Description);

        // Keep slug in sync (in-memory only)
        plant.Slug = string.IsNullOrWhiteSpace(plant.Slug) ? slug : plant.Slug;
        plant.PlanId = string.IsNullOrWhiteSpace(plant.PlanId) ? planId : plant.PlanId;

        return model;
    }

    public static Plant ToDomain(PlantFileModel model)
    {
        var (planId, slug) = FileSystemForestPaths.SplitPlantKey(model.Key);
        var created = TryParseRoundtripUtc(model.CreatedAt) ?? DateTime.UtcNow;
        var updated = TryParseRoundtripUtc(model.UpdatedAt);
        return new Plant
        {
            Key = model.Key ?? string.Empty,
            Slug = slug,
            PlanId = string.IsNullOrWhiteSpace(model.PlanId) ? planId : model.PlanId,
            PlannerId = model.PlannerId ?? string.Empty,
            Status = string.IsNullOrWhiteSpace(model.Status) ? "planned" : model.Status,
            Title = model.Title ?? string.Empty,
            Description = model.Description ?? string.Empty,
            AssignedPlanters = (model.AssignedPlanters ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
            Branches = (model.Branches ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
            CreatedDate = created,
            LastActivityDate = updated
        };
    }

    private static DateTime? TryParseRoundtripUtc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
        {
            return dto.UtcDateTime;
        }

        return null;
    }
}

