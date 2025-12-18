namespace GitForest.Core.Services;

/// <summary>
/// Service for managing plants (repositories).
/// </summary>
public interface IPlantService
{
    Task<Plant?> GetPlantAsync(string name);
    Task<IEnumerable<Plant>> GetAllPlantsAsync();
    Task AddPlantAsync(Plant plant);
    Task RemovePlantAsync(string name);
}
