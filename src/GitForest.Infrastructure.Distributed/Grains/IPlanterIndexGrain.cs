namespace GitForest.Infrastructure.Distributed.Grains;

/// <summary>
/// Index grain for tracking Planter IDs
/// </summary>
public interface IPlanterIndexGrain : IGrainWithIntegerKey
{
    Task<IReadOnlyList<string>> GetAllIdsAsync();
    Task AddIdAsync(string id);
    Task RemoveIdAsync(string id);
}
