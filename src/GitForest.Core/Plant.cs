namespace GitForest.Core;

/// <summary>
/// Represents a plant in the forest - an individual git repository or tree.
/// </summary>
public class Plant
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime PlantedDate { get; set; }
    public string PlantedBy { get; set; } = string.Empty;
}
