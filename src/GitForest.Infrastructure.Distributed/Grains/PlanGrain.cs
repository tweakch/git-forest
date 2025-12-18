using GitForest.Core;

namespace GitForest.Infrastructure.Distributed.Grains;

/// <summary>
/// Orleans grain implementation for Plan entity
/// </summary>
public class PlanGrain : Grain, IPlanGrain
{
    private readonly IPersistentState<Plan?> _state;

    public PlanGrain([PersistentState("plan")] IPersistentState<Plan?> state)
    {
        _state = state;
    }

    public Task<Plan?> GetAsync()
    {
        var plan = _state.State;
        if (plan is null)
            return Task.FromResult<Plan?>(null);

        // Some storage providers initialize reference types to a default instance
        // even when no record exists yet. Treat an empty Id as "no plan stored".
        if (string.IsNullOrWhiteSpace(plan.Id))
            return Task.FromResult<Plan?>(null);

        return Task.FromResult<Plan?>(plan);
    }

    public async Task SetAsync(Plan plan)
    {
        if (plan is null)
            throw new ArgumentNullException(nameof(plan));

        var key = this.GetPrimaryKeyString();
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Plan grain key was not available.");

        // Keep persisted state consistent with grain identity.
        if (string.IsNullOrWhiteSpace(plan.Id))
            plan.Id = key;
        else if (!string.Equals(plan.Id.Trim(), key, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Plan.Id '{plan.Id}' does not match grain key '{key}'."
            );

        _state.State = plan;
        await _state.WriteStateAsync();
    }

    public async Task DeleteAsync()
    {
        await _state.ClearStateAsync();
    }
}
