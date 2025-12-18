using GitForest.Core;

namespace GitForest.Infrastructure.Distributed.Grains;

/// <summary>
/// Orleans grain implementation for Plant entity
/// </summary>
public class PlantGrain : Grain, IPlantGrain
{
    private readonly IPersistentState<Plant?> _state;

    public PlantGrain([PersistentState("plant")] IPersistentState<Plant?> state)
    {
        _state = state;
    }

    public Task<Plant?> GetAsync()
    {
        var plant = _state.State;
        if (plant is null)
            return Task.FromResult<Plant?>(null);

        // Some storage providers initialize reference types to a default instance
        // even when no record exists yet. Treat an empty Key as "no plant stored".
        if (string.IsNullOrWhiteSpace(plant.Key))
            return Task.FromResult<Plant?>(null);

        return Task.FromResult<Plant?>(plant);
    }

    public async Task SetAsync(Plant plant)
    {
        if (plant is null)
            throw new ArgumentNullException(nameof(plant));

        var key = this.GetPrimaryKeyString();
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Plant grain key was not available.");

        // Keep persisted state consistent with grain identity.
        if (string.IsNullOrWhiteSpace(plant.Key))
            plant.Key = key;
        else if (!string.Equals(plant.Key.Trim(), key, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Plant.Key '{plant.Key}' does not match grain key '{key}'.");

        _state.State = plant;
        await _state.WriteStateAsync();
    }

    public async Task DeleteAsync()
    {
        await _state.ClearStateAsync();
    }
}
