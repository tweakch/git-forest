using GitForest.Core;

namespace GitForest.Core.Services;

/// <summary>
/// Service for managing planters (contributors).
/// </summary>
public interface IPlanterService
{
    Task<Planter?> GetPlanterAsync(string name);
    Task<IEnumerable<Planter>> GetAllPlantersAsync();
    Task AddPlanterAsync(Planter planter);
}
