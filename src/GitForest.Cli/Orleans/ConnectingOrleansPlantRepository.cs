using Ardalis.Specification;
using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Infrastructure.Distributed.Repositories;

namespace GitForest.Cli.Orleans;

internal sealed class ConnectingOrleansPlantRepository : IPlantRepository
{
    private readonly OrleansClientAccessor _client;
    private OrleansPlantRepository? _inner;

    public ConnectingOrleansPlantRepository(OrleansClientAccessor client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    private OrleansPlantRepository Inner =>
        _inner ??= new OrleansPlantRepository(_client.GrainFactory);

    private Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        return _client.EnsureConnectedAsync(TimeSpan.FromSeconds(5), cancellationToken);
    }

    public async Task<Plant?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        return await Inner.GetByIdAsync(id, cancellationToken);
    }

    public async Task<Plant?> GetBySpecAsync(
        ISpecification<Plant> specification,
        CancellationToken cancellationToken = default
    )
    {
        await EnsureConnectedAsync(cancellationToken);
        return await Inner.GetBySpecAsync(specification, cancellationToken);
    }

    public async Task<TResult?> GetBySpecAsync<TResult>(
        ISpecification<Plant, TResult> specification,
        CancellationToken cancellationToken = default
    )
    {
        await EnsureConnectedAsync(cancellationToken);
        return await Inner.GetBySpecAsync(specification, cancellationToken);
    }

    public async Task<IReadOnlyList<Plant>> ListAsync(
        ISpecification<Plant> specification,
        CancellationToken cancellationToken = default
    )
    {
        await EnsureConnectedAsync(cancellationToken);
        return await Inner.ListAsync(specification, cancellationToken);
    }

    public async Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        ISpecification<Plant, TResult> specification,
        CancellationToken cancellationToken = default
    )
    {
        await EnsureConnectedAsync(cancellationToken);
        return await Inner.ListAsync(specification, cancellationToken);
    }

    public async Task AddAsync(Plant entity, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await Inner.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(Plant entity, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await Inner.UpdateAsync(entity, cancellationToken);
    }

    public async Task DeleteAsync(Plant entity, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await Inner.DeleteAsync(entity, cancellationToken);
    }
}
