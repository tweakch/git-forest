namespace GitForest.Core.Services;

/// <summary>
/// Reads plans from the catalog (config/plans directory) before they are installed.
/// </summary>
public interface ICatalogPlanReader
{
    /// <summary>
    /// Lists all available plans from the catalog.
    /// </summary>
    Task<IReadOnlyList<CatalogPlan>> ListCatalogPlansAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets a specific plan from the catalog by its ID.
    /// </summary>
    Task<CatalogPlan?> GetCatalogPlanByIdAsync(
        string planId,
        CancellationToken cancellationToken = default
    );
}
