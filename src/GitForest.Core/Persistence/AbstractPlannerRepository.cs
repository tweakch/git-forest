using Ardalis.Specification;

namespace GitForest.Core.Persistence;

/// <summary>
/// Abstract base class for Planner repositories that provides common functionality.
/// </summary>
public abstract class AbstractPlannerRepository : IPlannerRepository
{
    // Abstract method for derived classes to load all planners
    protected abstract List<Planner> LoadAll();

    // Abstract CRUD methods that derived classes must implement
    public abstract Task<Planner?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    public abstract Task AddAsync(Planner entity, CancellationToken cancellationToken = default);
    public abstract Task UpdateAsync(Planner entity, CancellationToken cancellationToken = default);
    public abstract Task DeleteAsync(Planner entity, CancellationToken cancellationToken = default);

    // Common specification query methods
    public Task<Planner?> GetBySpecAsync(ISpecification<Planner> specification, CancellationToken cancellationToken = default)
        => GetBySpecInternalAsync(specification, cancellationToken);

    public Task<TResult?> GetBySpecAsync<TResult>(ISpecification<Planner, TResult> specification, CancellationToken cancellationToken = default)
        => GetBySpecInternalAsync(specification, cancellationToken);

    public Task<IReadOnlyList<Planner>> ListAsync(ISpecification<Planner> specification, CancellationToken cancellationToken = default)
        => ListBySpecInternalAsync(specification, cancellationToken);

    public Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<Planner, TResult> specification, CancellationToken cancellationToken = default)
        => ListBySpecInternalAsync(specification, cancellationToken);

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

    // Common specification evaluation methods
    private Task<TResult?> GetBySpecInternalAsync<TResult>(ISpecification<Planner, TResult> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAll();
        return Task.FromResult(SpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<Planner?> GetBySpecInternalAsync(ISpecification<Planner> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAll();
        return Task.FromResult(SpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<IReadOnlyList<TResult>> ListBySpecInternalAsync<TResult>(ISpecification<Planner, TResult> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAll();
        return Task.FromResult((IReadOnlyList<TResult>)SpecificationEvaluator.Apply(all, specification).ToList());
    }

    private Task<IReadOnlyList<Planner>> ListBySpecInternalAsync(ISpecification<Planner> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAll();
        return Task.FromResult((IReadOnlyList<Planner>)SpecificationEvaluator.Apply(all, specification).ToList());
    }
}
