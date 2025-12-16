namespace GitForest.Core;

/// <summary>
/// Represents a plan from the catalog (config/plans directory) - not yet installed.
/// Contains additional metadata like name, description, and category.
/// </summary>
public class CatalogPlan
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string Homepage { get; set; } = string.Empty;
    public List<string> Planners { get; set; } = new();
    public List<string> Planters { get; set; } = new();
    public List<string> Scopes { get; set; } = new();
}
