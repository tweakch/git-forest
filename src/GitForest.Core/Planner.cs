namespace GitForest.Core;

/// <summary>
/// Represents a planner - an organizer/manager who coordinates the forest.
/// </summary>
public class Planner
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> ManagedForests { get; set; } = new();
    public string Role { get; set; } = string.Empty;
}
