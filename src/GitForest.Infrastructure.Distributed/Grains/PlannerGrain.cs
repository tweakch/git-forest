using GitForest.Core;
using Orleans.Runtime;

namespace GitForest.Infrastructure.Distributed.Grains;

/// <summary>
/// Orleans grain implementation for Planner entity
/// </summary>
public class PlannerGrain : Grain, IPlannerGrain
{
    private readonly IPersistentState<Planner?> _state;

    public PlannerGrain([PersistentState("planner")] IPersistentState<Planner?> state)
    {
        _state = state;
    }

    public Task<Planner?> GetAsync()
    {
        var planner = _state.State;
        if (planner is null)
            return Task.FromResult<Planner?>(null);

        // Some storage providers initialize reference types to a default instance
        // even when no record exists yet. Treat an empty Id as "no planner stored".
        if (string.IsNullOrWhiteSpace(planner.Id))
            return Task.FromResult<Planner?>(null);

        return Task.FromResult<Planner?>(planner);
    }

    public async Task SetAsync(Planner planner)
    {
        if (planner is null)
            throw new ArgumentNullException(nameof(planner));

        var key = this.GetPrimaryKeyString();
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Planner grain key was not available.");

        // Keep persisted state consistent with grain identity.
        if (string.IsNullOrWhiteSpace(planner.Id))
            planner.Id = key;
        else if (!string.Equals(planner.Id.Trim(), key, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Planner.Id '{planner.Id}' does not match grain key '{key}'.");

        _state.State = planner;
        await _state.WriteStateAsync();
    }

    public async Task DeleteAsync()
    {
        await _state.ClearStateAsync();
    }
}
