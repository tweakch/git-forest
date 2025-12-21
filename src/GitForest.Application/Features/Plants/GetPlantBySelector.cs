using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Mediator;

namespace GitForest.Application.Features.Plants;

public sealed record GetPlantBySelectorQuery(string Selector) : IRequest<Plant>;

internal sealed class GetPlantBySelectorHandler : IRequestHandler<GetPlantBySelectorQuery, Plant>
{
    private readonly IPlantRepository _plants;

    public GetPlantBySelectorHandler(IPlantRepository plants)
    {
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
    }

    public Task<Plant> Handle(GetPlantBySelectorQuery request, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return PlantSelector.ResolveAsync(_plants, request.Selector, cancellationToken);
    }
}

