namespace GitForest.Core;

/// <summary>
/// Represents a planner - a deterministic generator that produces a desired set of Plants from a Plan.
/// </summary>
public class Planner
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
}
