using GitForest.Core;

namespace GitForest.Infrastructure.Distributed.Grains;

/// <summary>
/// Orleans grain interface for Planter entity
/// </summary>
public interface IPlanterGrain : IGrainWithStringKey
{
    /// <summary>
    /// Gets the planter
    /// </summary>
    Task<Planter?> GetAsync();

    /// <summary>
    /// Sets or updates the planter
    /// </summary>
    Task SetAsync(Planter planter);

    /// <summary>
    /// Deletes the planter
    /// </summary>
    Task DeleteAsync();
}
