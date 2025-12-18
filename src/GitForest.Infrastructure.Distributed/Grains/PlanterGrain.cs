using GitForest.Core;
using Orleans.Runtime;

namespace GitForest.Infrastructure.Distributed.Grains;

/// <summary>
/// Orleans grain implementation for Planter entity
/// </summary>
public class PlanterGrain : Grain, IPlanterGrain
{
    private readonly IPersistentState<Planter?> _state;

    public PlanterGrain([PersistentState("planter")] IPersistentState<Planter?> state)
    {
        _state = state;
    }

    public Task<Planter?> GetAsync()
    {
        var planter = _state.State;
        if (planter is null)
            return Task.FromResult<Planter?>(null);

        // Some storage providers initialize reference types to a default instance
        // even when no record exists yet. Treat an empty Id as "no planter stored".
        if (string.IsNullOrWhiteSpace(planter.Id))
            return Task.FromResult<Planter?>(null);

        return Task.FromResult<Planter?>(planter);
    }

    public async Task SetAsync(Planter planter)
    {
        if (planter is null)
            throw new ArgumentNullException(nameof(planter));

        var key = this.GetPrimaryKeyString();
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Planter grain key was not available.");

        // Keep persisted state consistent with grain identity.
        if (string.IsNullOrWhiteSpace(planter.Id))
            planter.Id = key;
        else if (!string.Equals(planter.Id.Trim(), key, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Planter.Id '{planter.Id}' does not match grain key '{key}'.");

        _state.State = planter;
        await _state.WriteStateAsync();
    }

    public async Task DeleteAsync()
    {
        await _state.ClearStateAsync();
    }
}
