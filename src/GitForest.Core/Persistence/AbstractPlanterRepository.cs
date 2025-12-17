using Ardalis.Specification;

namespace GitForest.Core.Persistence;

/// <summary>
/// Abstract base class for Planter repositories that provides common functionality.
/// </summary>
public abstract class AbstractPlanterRepository : IPlanterRepository
{
    // Abstract method for derived classes to load all planters
    protected abstract List<Planter> LoadAll();

    // Abstract CRUD methods that derived classes must implement
    public abstract Task<Planter?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    public abstract Task AddAsync(Planter entity, CancellationToken cancellationToken = default);
    public abstract Task UpdateAsync(Planter entity, CancellationToken cancellationToken = default);
    public abstract Task DeleteAsync(Planter entity, CancellationToken cancellationToken = default);

    // Common specification query methods
    public Task<Planter?> GetBySpecAsync(ISpecification<Planter> specification, CancellationToken cancellationToken = default)
        => GetBySpecInternalAsync(specification, cancellationToken);

    public Task<TResult?> GetBySpecAsync<TResult>(ISpecification<Planter, TResult> specification, CancellationToken cancellationToken = default)
        => GetBySpecInternalAsync(specification, cancellationToken);

    public Task<IReadOnlyList<Planter>> ListAsync(ISpecification<Planter> specification, CancellationToken cancellationToken = default)
        => ListBySpecInternalAsync(specification, cancellationToken);

    public Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<Planter, TResult> specification, CancellationToken cancellationToken = default)
        => ListBySpecInternalAsync(specification, cancellationToken);

    // Common validation methods
    protected void ValidateEntity(Planter entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Planter.Id must be provided.", nameof(entity));
    }

    protected string GetTrimmedId(Planter entity)
    {
        // ValidateEntity ensures entity.Id is not null or whitespace
        return entity.Id!.Trim();
    }

    // Common specification evaluation methods
    private Task<TResult?> GetBySpecInternalAsync<TResult>(ISpecification<Planter, TResult> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAll();
        return Task.FromResult(SpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<Planter?> GetBySpecInternalAsync(ISpecification<Planter> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAll();
        return Task.FromResult(SpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<IReadOnlyList<TResult>> ListBySpecInternalAsync<TResult>(ISpecification<Planter, TResult> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAll();
        return Task.FromResult((IReadOnlyList<TResult>)SpecificationEvaluator.Apply(all, specification).ToList());
    }

    private Task<IReadOnlyList<Planter>> ListBySpecInternalAsync(ISpecification<Planter> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAll();
        return Task.FromResult((IReadOnlyList<Planter>)SpecificationEvaluator.Apply(all, specification).ToList());
    }
}
