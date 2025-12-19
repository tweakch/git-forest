namespace GitForest.Core;

/// <summary>
/// Represents a plant - a concrete work item with stable key and lifecycle facts.
/// </summary>
public class Plant
{
    /// <summary>
    /// Stable plant key in format: planId:plantSlug (e.g., sample:backend-memory-hygiene)
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Plant slug (unique within a plan)
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Plan ID this plant belongs to
    /// </summary>
    public string PlanId { get; set; } = string.Empty;

    /// <summary>
    /// Planner ID that created/owns this plant (if known).
    /// </summary>
    public string PlannerId { get; set; } = string.Empty;

    /// <summary>
    /// Status: planned, planted, growing, harvestable, harvested, archived
    /// </summary>
    public string Status { get; set; } = "planned";

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> AssignedPlanters { get; set; } = new();
    public List<string> Branches { get; set; } = new();
    public string? SelectedBranch { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastActivityDate { get; set; }
}
