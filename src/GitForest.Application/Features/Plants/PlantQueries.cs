using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Core.Specifications.Plants;
using GitForest.Mediator;

namespace GitForest.Application.Features.Plants;

public sealed record ListPlantsQuery(string? Status, string? PlanId)
    : IRequest<IReadOnlyList<Plant>>;

internal sealed class ListPlantsHandler : IRequestHandler<ListPlantsQuery, IReadOnlyList<Plant>>
{
    private readonly IPlantRepository _plants;

    public ListPlantsHandler(IPlantRepository plants)
    {
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
    }

    public async Task<IReadOnlyList<Plant>> Handle(
        ListPlantsQuery request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var status = string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim();
        var planId = string.IsNullOrWhiteSpace(request.PlanId) ? null : request.PlanId.Trim();

        if (status is null && planId is null)
        {
            return await _plants.ListAsync(new AllPlantsSpec(), cancellationToken);
        }

        if (status is not null && planId is null)
        {
            return await _plants.ListAsync(new PlantsByStatusSpec(status), cancellationToken);
        }

        if (status is null)
        {
            return await _plants.ListAsync(new PlantsByPlanIdSpec(planId!), cancellationToken);
        }

        return await _plants.ListAsync(
            new PlantsByPlanIdAndStatusSpec(planId!, status),
            cancellationToken
        );
    }
}

public sealed record GetPlantByKeyQuery(string Key) : IRequest<Plant?>;

internal sealed class GetPlantByKeyHandler : IRequestHandler<GetPlantByKeyQuery, Plant?>
{
    private readonly IPlantRepository _plants;

    public GetPlantByKeyHandler(IPlantRepository plants)
    {
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
    }

    public Task<Plant?> Handle(GetPlantByKeyQuery request, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Key))
            return Task.FromResult<Plant?>(null);

        return _plants.GetBySpecAsync(new PlantByKeySpec(request.Key.Trim()), cancellationToken);
    }
}
