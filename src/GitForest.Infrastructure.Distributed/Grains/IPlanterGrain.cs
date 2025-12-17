namespace GitForest.Infrastructure.Distributed.Grains;

/// <summary>
/// Orleans grain interface for distributed planter execution.
/// Planters are executor personas (agents) that propose diffs/PRs for plants under policies.
/// </summary>
public interface IPlanterGrain : IGrainWithStringKey
{
    /// <summary>
    /// Executes the planter to process a plant and propose changes.
    /// </summary>
    /// <param name="plantKey">The unique key of the plant to process.</param>
    /// <param name="mode">The execution mode (propose vs apply).</param>
    /// <returns>The result of the planter execution.</returns>
    Task<PlanterExecutionResult> ExecutePlanterAsync(string plantKey, ExecutionMode mode);
    
    /// <summary>
    /// Gets the current status and capacity of the planter.
    /// </summary>
    Task<PlanterStatus> GetStatusAsync();
    
    /// <summary>
    /// Assigns a plant to this planter.
    /// </summary>
    Task AssignPlantAsync(string plantKey);
}

/// <summary>
/// Represents the execution mode for a planter.
/// </summary>
public enum ExecutionMode
{
    Propose,
    Apply
}

/// <summary>
/// Represents the result of a planter execution.
/// </summary>
public record PlanterExecutionResult(
    string PlantKey,
    bool Success,
    string? DiffOrPullRequestUrl,
    string? ErrorMessage);

/// <summary>
/// Represents the status of a planter grain.
/// </summary>
public record PlanterStatus(
    string PlanterId,
    bool IsRunning,
    int AssignedPlantCount,
    int CapacityLimit,
    DateTime? LastExecutionTime);
