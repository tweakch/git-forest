namespace GitForest.Core;

/// <summary>
/// Represents a planter - a contributor/user who plants repositories.
/// </summary>
public class Planter
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> PlantedRepositories { get; set; } = new();
}
