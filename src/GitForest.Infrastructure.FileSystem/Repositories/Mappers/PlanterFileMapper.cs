using GitForest.Core;
using GitForest.Infrastructure.FileSystem.Serialization;

namespace GitForest.Infrastructure.FileSystem.Repositories;

internal static class PlanterFileMapper
{
    public static PlanterFileModel ToFileModel(Planter planter)
    {
        if (planter is null)
            throw new ArgumentNullException(nameof(planter));

        var id = planter.Id.Trim();
        return new PlanterFileModel(
            Id: id,
            Name: planter.Name ?? string.Empty,
            Type: planter.Type ?? "builtin",
            Origin: planter.Origin ?? "plan",
            AssignedPlants: planter.AssignedPlants ?? new List<string>(),
            IsActive: planter.IsActive
        );
    }

    public static Planter ToDomain(PlanterFileModel model, string fallbackId)
    {
        return new Planter
        {
            Id = string.IsNullOrWhiteSpace(model.Id) ? fallbackId : model.Id,
            Name = model.Name ?? string.Empty,
            Type = string.IsNullOrWhiteSpace(model.Type) ? "builtin" : model.Type,
            Origin = string.IsNullOrWhiteSpace(model.Origin) ? "plan" : model.Origin,
            AssignedPlants = (model.AssignedPlants ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList(),
            IsActive = model.IsActive,
        };
    }
}
