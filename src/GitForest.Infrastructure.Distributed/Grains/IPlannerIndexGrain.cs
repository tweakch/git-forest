namespace GitForest.Infrastructure.Distributed.Grains;

/// <summary>
/// Index grain for tracking Planner IDs
/// </summary>
public interface IPlannerIndexGrain : IGrainWithIntegerKey
{
    Task<IReadOnlyList<string>> GetAllIdsAsync();
    Task AddIdAsync(string id);
    Task RemoveIdAsync(string id);
}
