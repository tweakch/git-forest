namespace GitForest.Core;

/// <summary>
/// Represents a planter - an executor persona (agent) that proposes diffs/PRs for plants.
/// </summary>
public class Planter
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "builtin"; // builtin or custom
    public string Origin { get; set; } = "plan"; // plan or user
    public List<string> AssignedPlants { get; set; } = new();
    public bool IsActive { get; set; } = false;
}
