using GitForest.Core;

namespace GitForest.Infrastructure.Distributed.Grains;

/// <summary>
/// Orleans grain interface for Plant entity
/// </summary>
public interface IPlantGrain : IGrainWithStringKey
{
    /// <summary>
    /// Gets the plant
    /// </summary>
    Task<Plant?> GetAsync();

    /// <summary>
    /// Sets or updates the plant
    /// </summary>
    Task SetAsync(Plant plant);

    /// <summary>
    /// Deletes the plant
    /// </summary>
    Task DeleteAsync();
}
