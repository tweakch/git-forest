using GitForest.Mediator;

namespace GitForest.Application.Features.Planners;

public sealed record RunPlannerCommand(string PlannerId, string PlanId)
    : IRequest<RunPlannerResult>;

public sealed record RunPlannerResult(string PlannerId, string PlanId, string Status);

internal sealed class RunPlannerHandler : IRequestHandler<RunPlannerCommand, RunPlannerResult>
{
    public Task<RunPlannerResult> Handle(
        RunPlannerCommand request,
        CancellationToken cancellationToken
    )
    {
        _ = cancellationToken;
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // Placeholder: current CLI always reports "completed".
        return Task.FromResult(
            new RunPlannerResult(request.PlannerId, request.PlanId, Status: "completed")
        );
    }
}
