namespace GitForest.Infrastructure.Distributed.Grains;

/// <summary>
/// Index grain for tracking Plant IDs
/// </summary>
public interface IPlantIndexGrain : IGrainWithIntegerKey
{
    Task<IReadOnlyList<string>> GetAllIdsAsync();
    Task AddIdAsync(string id);
    Task RemoveIdAsync(string id);
}
