using GitForest.Core;

namespace GitForest.Core.Services;

/// <summary>
/// Service for managing plans.
/// </summary>
public interface IPlanService
{
    Task<Plan?> GetPlanAsync(string id);
    Task<IEnumerable<Plan>> GetAllPlansAsync();
    Task InstallPlanAsync(Plan plan);
    Task RemovePlanAsync(string id, bool purgePlants);
    Task ReconcilePlanAsync(string id, bool update, bool dryRun);
}
