using GitForest.Core;
using Orleans.Runtime;

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
        return Task.FromResult(_state.State);
    }

    public async Task SetAsync(Plant plant)
    {
        _state.State = plant;
        await _state.WriteStateAsync();
    }

    public async Task DeleteAsync()
    {
        await _state.ClearStateAsync();
    }
}
