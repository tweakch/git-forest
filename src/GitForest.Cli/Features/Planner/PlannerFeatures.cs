using MediatR;

namespace GitForest.Cli.Features.Planner;

public sealed record RunPlannerCommand(string PlannerId, string PlanId) : IRequest<RunPlannerResult>;

public sealed record RunPlannerResult(string PlannerId, string PlanId, string Status);

internal sealed class RunPlannerHandler : IRequestHandler<RunPlannerCommand, RunPlannerResult>
{
    public Task<RunPlannerResult> Handle(RunPlannerCommand request, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        // Placeholder: current CLI always reports "completed".
        return Task.FromResult(new RunPlannerResult(request.PlannerId, request.PlanId, Status: "completed"));
    }
}

