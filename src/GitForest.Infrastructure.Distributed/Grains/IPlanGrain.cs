using GitForest.Core;

namespace GitForest.Infrastructure.Distributed.Grains;

/// <summary>
/// Orleans grain interface for Plan entity
/// </summary>
public interface IPlanGrain : IGrainWithStringKey
{
    /// <summary>
    /// Gets the plan
    /// </summary>
    Task<Plan?> GetAsync();

    /// <summary>
    /// Sets or updates the plan
    /// </summary>
    Task SetAsync(Plan plan);

    /// <summary>
    /// Deletes the plan
    /// </summary>
    Task DeleteAsync();
}
