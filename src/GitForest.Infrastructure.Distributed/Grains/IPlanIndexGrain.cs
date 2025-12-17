namespace GitForest.Infrastructure.Distributed.Grains;

/// <summary>
/// Index grain for tracking Plan IDs
/// </summary>
public interface IPlanIndexGrain : IGrainWithIntegerKey
{
    Task<IReadOnlyList<string>> GetAllIdsAsync();
    Task AddIdAsync(string id);
    Task RemoveIdAsync(string id);
}
