namespace GitForest.Core.Services;

/// <summary>
/// Core port for executing an "agent" chat interaction (typically backed by an LLM).
/// Implementations live in infrastructure; Core only defines the contract.
/// </summary>
public interface IAgentChatClient
{
    Task<AgentChatResponse> ChatAsync(AgentChatRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Core port for running a reconciliation "forum" across multiple agents (planners/planters)
/// to produce a desired set of plants for a plan.
/// </summary>
public interface IReconciliationForum
{
    Task<ReconciliationStrategy> RunAsync(ReconcileContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Input to an agent chat call.
/// </summary>
/// <param name="AgentId">Logical agent id (planner/planter/moderator) used for routing/telemetry.</param>
/// <param name="SystemPrompt">System prompt / role instructions for the agent.</param>
/// <param name="UserPrompt">User prompt content for the agent.</param>
/// <param name="Temperature">Optional temperature override; forum should typically force determinism (e.g. 0).</param>
/// <param name="Model">Optional model override.</param>
/// <param name="Metadata">Optional metadata for tracing/debugging (non-functional).</param>
public sealed record AgentChatRequest(
    string AgentId,
    string SystemPrompt,
    string UserPrompt,
    double? Temperature = null,
    string? Model = null,
    IReadOnlyDictionary<string, string>? Metadata = null
);

/// <summary>
/// Output from an agent chat call.
/// </summary>
/// <param name="RawContent">Raw assistant message content (may contain JSON).</param>
/// <param name="Json">Optional extracted JSON payload if the response is structured.</param>
public sealed record AgentChatResponse(
    string RawContent,
    string? Json = null
);

/// <summary>
/// Context passed into reconciliation. This is intentionally persistence-agnostic.
/// </summary>
/// <param name="PlanId">Target plan id being reconciled.</param>
/// <param name="Plan">Parsed plan model.</param>
/// <param name="RawPlanYaml">Raw plan YAML as installed (optional but helpful for prompt fidelity).</param>
/// <param name="ExistingPlants">Snapshot of current plants for the plan (stable order preferred by caller).</param>
/// <param name="Repository">Optional repository identifier (e.g. "org/repo") if available.</param>
public sealed record ReconcileContext(
    string PlanId,
    Plan Plan,
    string? RawPlanYaml,
    IReadOnlyList<Plant> ExistingPlants,
    string? Repository = null
);

/// <summary>
/// A desired plant output by a reconciliation strategy.
/// </summary>
/// <param name="Key">Stable plant key (typically planId:slug).</param>
/// <param name="Slug">Plant slug (unique within a plan).</param>
/// <param name="Title">Human-friendly title.</param>
/// <param name="Description">Longer description / rationale.</param>
/// <param name="PlannerId">Planner id responsible for this plant.</param>
/// <param name="AssignedPlanters">Planter ids assigned to execute this plant.</param>
public sealed record DesiredPlant(
    string Key,
    string Slug,
    string Title,
    string Description,
    string PlannerId,
    IReadOnlyList<string> AssignedPlanters
);

/// <summary>
/// Result of reconciliation: the desired plant set + optional explanation/metadata.
/// </summary>
/// <param name="DesiredPlants">0..n desired plants.</param>
/// <param name="Summary">Optional human-readable summary of the reconciliation.</param>
/// <param name="Metadata">Optional structured metadata for debugging/telemetry.</param>
public sealed record ReconciliationStrategy(
    IReadOnlyList<DesiredPlant> DesiredPlants,
    string? Summary = null,
    IReadOnlyDictionary<string, string>? Metadata = null
);

