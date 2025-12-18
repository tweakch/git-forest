namespace GitForest.Core.Persistence;

/// <summary>
/// Abstract base class for Planter repositories that provides common functionality.
/// </summary>
public abstract class AbstractPlanterRepository
    : AbstractRepositoryWithSpecs<Planter, string>,
        IPlanterRepository
{
    // Abstract method for derived classes to load all planters
    protected abstract List<Planter> LoadAll();

    // Abstract CRUD methods that derived classes must implement
    public abstract override Task<Planter?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default
    );
    public abstract override Task AddAsync(
        Planter entity,
        CancellationToken cancellationToken = default
    );
    public abstract override Task UpdateAsync(
        Planter entity,
        CancellationToken cancellationToken = default
    );
    public abstract override Task DeleteAsync(
        Planter entity,
        CancellationToken cancellationToken = default
    );

    // Common validation methods
    protected void ValidateEntity(Planter entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id))
            throw new ArgumentException("Planter.Id must be provided.", nameof(entity));
    }

    protected string GetTrimmedId(Planter entity)
    {
        // ValidateEntity ensures entity.Id is not null or whitespace
        return entity.Id!.Trim();
    }

    protected override Task<IReadOnlyList<Planter>> LoadAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        _ = cancellationToken;
        return Task.FromResult((IReadOnlyList<Planter>)LoadAll());
    }
}
