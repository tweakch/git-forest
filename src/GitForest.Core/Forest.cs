namespace GitForest.Core;

/// <summary>
/// Represents a forest - a collection of plants (git repositories).
/// </summary>
public class Forest
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public List<Plant> Plants { get; set; } = new();
    public DateTime CreatedDate { get; set; }
}
