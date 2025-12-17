using Ardalis.Specification;

namespace GitForest.Core.Persistence;

/// <summary>
/// Abstract base class for Plant repositories that provides common functionality.
/// </summary>
public abstract class AbstractPlantRepository : IPlantRepository
{
    // Abstract method for derived classes to load all plants
    protected abstract List<Plant> LoadAll();

    // Abstract CRUD methods that derived classes must implement
    public abstract Task<Plant?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    public abstract Task AddAsync(Plant entity, CancellationToken cancellationToken = default);
    public abstract Task UpdateAsync(Plant entity, CancellationToken cancellationToken = default);
    public abstract Task DeleteAsync(Plant entity, CancellationToken cancellationToken = default);

    // Common specification query methods
    public Task<Plant?> GetBySpecAsync(ISpecification<Plant> specification, CancellationToken cancellationToken = default)
        => GetBySpecInternalAsync(specification, cancellationToken);

    public Task<TResult?> GetBySpecAsync<TResult>(ISpecification<Plant, TResult> specification, CancellationToken cancellationToken = default)
        => GetBySpecInternalAsync(specification, cancellationToken);

    public Task<IReadOnlyList<Plant>> ListAsync(ISpecification<Plant> specification, CancellationToken cancellationToken = default)
        => ListBySpecInternalAsync(specification, cancellationToken);

    public Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<Plant, TResult> specification, CancellationToken cancellationToken = default)
        => ListBySpecInternalAsync(specification, cancellationToken);

    // Common validation methods
    protected void ValidateEntity(Plant entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Key)) throw new ArgumentException("Plant.Key must be provided.", nameof(entity));
    }

    protected string GetTrimmedId(Plant entity)
    {
        // ValidateEntity ensures entity.Key is not null or whitespace
        return entity.Key!.Trim();
    }

    // Common specification evaluation methods
    private Task<TResult?> GetBySpecInternalAsync<TResult>(ISpecification<Plant, TResult> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAll();
        return Task.FromResult(SpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<Plant?> GetBySpecInternalAsync(ISpecification<Plant> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAll();
        return Task.FromResult(SpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<IReadOnlyList<TResult>> ListBySpecInternalAsync<TResult>(ISpecification<Plant, TResult> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAll();
        return Task.FromResult((IReadOnlyList<TResult>)SpecificationEvaluator.Apply(all, specification).ToList());
    }

    private Task<IReadOnlyList<Plant>> ListBySpecInternalAsync(ISpecification<Plant> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAll();
        return Task.FromResult((IReadOnlyList<Plant>)SpecificationEvaluator.Apply(all, specification).ToList());
    }
}
