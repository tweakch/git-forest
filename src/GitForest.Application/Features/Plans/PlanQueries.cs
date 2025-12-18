using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Core.Specifications.Plans;
using MediatR;

namespace GitForest.Application.Features.Plans;

public sealed record ListPlansQuery() : IRequest<IReadOnlyList<Plan>>;

internal sealed class ListPlansHandler : IRequestHandler<ListPlansQuery, IReadOnlyList<Plan>>
{
    private readonly IPlanRepository _plans;

    public ListPlansHandler(IPlanRepository plans)
    {
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
    }

    public Task<IReadOnlyList<Plan>> Handle(
        ListPlansQuery request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        return _plans.ListAsync(new AllPlansSpec(), cancellationToken);
    }
}

public sealed record GetPlanByIdQuery(string PlanId) : IRequest<Plan?>;

internal sealed class GetPlanByIdHandler : IRequestHandler<GetPlanByIdQuery, Plan?>
{
    private readonly IPlanRepository _plans;

    public GetPlanByIdHandler(IPlanRepository plans)
    {
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
    }

    public Task<Plan?> Handle(GetPlanByIdQuery request, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.PlanId))
            return Task.FromResult<Plan?>(null);

        return _plans.GetBySpecAsync(new PlanByIdSpec(request.PlanId.Trim()), cancellationToken);
    }
}
