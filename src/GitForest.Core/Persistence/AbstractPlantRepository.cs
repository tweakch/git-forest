namespace GitForest.Core.Persistence;

/// <summary>
/// Abstract base class for Plant repositories that provides common functionality.
/// </summary>
public abstract class AbstractPlantRepository
    : AbstractRepositoryWithSpecs<Plant, string>,
        IPlantRepository
{
    // Abstract method for derived classes to load all plants
    protected abstract List<Plant> LoadAll();

    // Abstract CRUD methods that derived classes must implement
    public abstract override Task<Plant?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default
    );
    public abstract override Task AddAsync(
        Plant entity,
        CancellationToken cancellationToken = default
    );
    public abstract override Task UpdateAsync(
        Plant entity,
        CancellationToken cancellationToken = default
    );
    public abstract override Task DeleteAsync(
        Plant entity,
        CancellationToken cancellationToken = default
    );

    // Common validation methods
    protected void ValidateEntity(Plant entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Key))
            throw new ArgumentException("Plant.Key must be provided.", nameof(entity));
    }

    protected string GetTrimmedId(Plant entity)
    {
        // ValidateEntity ensures entity.Key is not null or whitespace
        return entity.Key!.Trim();
    }

    protected override Task<IReadOnlyList<Plant>> LoadAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        _ = cancellationToken;
        return Task.FromResult((IReadOnlyList<Plant>)LoadAll());
    }
}
