using GitForest.Core;

namespace GitForest.Core.Services;

/// <summary>
/// Service for managing planners (organizers/managers).
/// </summary>
public interface IPlannerService
{
    Task<Planner?> GetPlannerAsync(string name);
    Task<IEnumerable<Planner>> GetAllPlannersAsync();
    Task AddPlannerAsync(Planner planner);
}
