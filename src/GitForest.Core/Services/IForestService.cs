namespace GitForest.Core.Services;

/// <summary>
/// Service for managing the forest and its configuration.
/// </summary>
public interface IForestService
{
    Task InitializeAsync(string path);
    Task<Forest?> GetCurrentForestAsync();
    Task SaveForestAsync(Forest forest);
}
