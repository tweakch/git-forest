namespace GitForest.Core.Persistence;

/// <summary>
/// Abstract base class for Planner repositories that provides common functionality.
/// </summary>
public abstract class AbstractPlannerRepository : AbstractRepositoryWithSpecs<Planner, string>, IPlannerRepository
{
    // Abstract method for derived classes to load all planners
    protected abstract List<Planner> LoadAll();

    // Abstract CRUD methods that derived classes must implement
    public abstract override Task<Planner?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    public abstract override Task AddAsync(Planner entity, CancellationToken cancellationToken = default);
    public abstract override Task UpdateAsync(Planner entity, CancellationToken cancellationToken = default);
    public abstract override Task DeleteAsync(Planner entity, CancellationToken cancellationToken = default);

    // Common validation methods
    protected void ValidateEntity(Planner entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Planner.Id must be provided.", nameof(entity));
    }

    protected string GetTrimmedId(Planner entity)
    {
        // ValidateEntity ensures entity.Id is not null or whitespace
        return entity.Id!.Trim();
    }

    protected override Task<IReadOnlyList<Planner>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return Task.FromResult((IReadOnlyList<Planner>)LoadAll());
    }
}
