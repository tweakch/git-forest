namespace GitForest.Core.Persistence;

/// <summary>
/// Abstract base class for Plan repositories that provides common functionality.
/// </summary>
public abstract class AbstractPlanRepository : AbstractRepositoryWithSpecs<Plan, string>, IPlanRepository
{
    // Abstract method for derived classes to load all plans
    protected abstract List<Plan> LoadAllPlans();

    // Abstract CRUD methods that derived classes must implement
    public abstract override Task<Plan?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    public abstract override Task AddAsync(Plan entity, CancellationToken cancellationToken = default);
    public abstract override Task UpdateAsync(Plan entity, CancellationToken cancellationToken = default);
    public abstract override Task DeleteAsync(Plan entity, CancellationToken cancellationToken = default);

    // Common validation methods
    protected void ValidateEntity(Plan entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Plan.Id must be provided.", nameof(entity));
    }

    protected string GetTrimmedId(Plan entity)
    {
        // ValidateEntity ensures entity.Id is not null or whitespace
        return entity.Id!.Trim();
    }

    protected override Task<IReadOnlyList<Plan>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return Task.FromResult((IReadOnlyList<Plan>)LoadAllPlans());
    }
}
