using Ardalis.Specification;

namespace GitForest.Core.Persistence;

/// <summary>
/// Abstract base class for Plan repositories that provides common functionality.
/// </summary>
public abstract class AbstractPlanRepository : IPlanRepository
{
    // Abstract method for derived classes to load all plans
    protected abstract List<Plan> LoadAllPlans();

    // Abstract CRUD methods that derived classes must implement
    public abstract Task<Plan?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    public abstract Task AddAsync(Plan entity, CancellationToken cancellationToken = default);
    public abstract Task UpdateAsync(Plan entity, CancellationToken cancellationToken = default);
    public abstract Task DeleteAsync(Plan entity, CancellationToken cancellationToken = default);

    // Common specification query methods
    public Task<Plan?> GetBySpecAsync(ISpecification<Plan> specification, CancellationToken cancellationToken = default)
        => GetBySpecInternalAsync(specification, cancellationToken);

    public Task<TResult?> GetBySpecAsync<TResult>(ISpecification<Plan, TResult> specification, CancellationToken cancellationToken = default)
        => GetBySpecInternalAsync(specification, cancellationToken);

    public Task<IReadOnlyList<Plan>> ListAsync(ISpecification<Plan> specification, CancellationToken cancellationToken = default)
        => ListBySpecInternalAsync(specification, cancellationToken);

    public Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<Plan, TResult> specification, CancellationToken cancellationToken = default)
        => ListBySpecInternalAsync(specification, cancellationToken);

    // Common validation methods
    protected void ValidateEntity(Plan entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Plan.Id must be provided.", nameof(entity));
    }

    protected string GetTrimmedId(Plan entity)
    {
        return entity.Id.Trim();
    }

    // Common specification evaluation methods
    private Task<TResult?> GetBySpecInternalAsync<TResult>(ISpecification<Plan, TResult> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAllPlans();
        return Task.FromResult(SpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<Plan?> GetBySpecInternalAsync(ISpecification<Plan> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAllPlans();
        return Task.FromResult(SpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<IReadOnlyList<TResult>> ListBySpecInternalAsync<TResult>(ISpecification<Plan, TResult> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAllPlans();
        return Task.FromResult((IReadOnlyList<TResult>)SpecificationEvaluator.Apply(all, specification).ToList());
    }

    private Task<IReadOnlyList<Plan>> ListBySpecInternalAsync(ISpecification<Plan> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = LoadAllPlans();
        return Task.FromResult((IReadOnlyList<Plan>)SpecificationEvaluator.Apply(all, specification).ToList());
    }
}
