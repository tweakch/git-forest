using GitForest.Core;

namespace GitForest.Infrastructure.Distributed.Grains;

/// <summary>
/// Orleans grain interface for Planner entity
/// </summary>
public interface IPlannerGrain : IGrainWithStringKey
{
    /// <summary>
    /// Gets the planner
    /// </summary>
    Task<Planner?> GetAsync();

    /// <summary>
    /// Sets or updates the planner
    /// </summary>
    Task SetAsync(Planner planner);

    /// <summary>
    /// Deletes the planner
    /// </summary>
    Task DeleteAsync();
}
