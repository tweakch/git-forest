namespace GitForest.Core;

/// <summary>
/// Represents a plan - a versioned package defining desired forest intent.
/// </summary>
public class Plan
{
    public string Id { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public List<string> Planners { get; set; } = new();
    public List<string> Planters { get; set; } = new();
    public DateTime InstalledDate { get; set; }
}
