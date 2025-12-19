using GitForest.Application.Features.Plans;
using GitForest.Core.Persistence;
using GitForest.Core.Services;
using GitForest.Core.Specifications.Plans;
using GitForest.Mediator;

namespace GitForest.Cli.Features.Planning;

public sealed record PlanForestCommand(string? PlanId, string? PlannerId, bool DryRun)
    : IRequest<PlanForestResult>;

public sealed record PlanForestResult(
    string? PlanId,
    string? PlannerId,
    int PlansPlanned,
    int PlantsCreated,
    int PlantsUpdated,
    IReadOnlyList<PlanPlanningResult> Plans,
    bool DryRun
);

public sealed record PlanPlanningResult(string PlanId, int PlantsCreated, int PlantsUpdated);

internal sealed class PlanForestHandler : IRequestHandler<PlanForestCommand, PlanForestResult>
{
    private readonly IPlanRepository _plans;
    private readonly IPlanReconciler _reconciler;

    public PlanForestHandler(IPlanRepository plans, IPlanReconciler reconciler)
    {
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _reconciler = reconciler ?? throw new ArgumentNullException(nameof(reconciler));
    }

    public async Task<PlanForestResult> Handle(
        PlanForestCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var planId = Normalize(request.PlanId);
        var plannerId = Normalize(request.PlannerId);

        var allPlans = await _plans.ListAsync(new AllPlansSpec(), cancellationToken);
        var filtered = allPlans.AsEnumerable();

        if (planId is not null)
        {
            var planExists = allPlans.Any(p =>
                string.Equals(p.Id, planId, StringComparison.OrdinalIgnoreCase)
            );
            if (!planExists)
            {
                throw new PlanNotInstalledException(planId);
            }

            filtered = filtered.Where(p =>
                string.Equals(p.Id, planId, StringComparison.OrdinalIgnoreCase)
            );
        }

        if (plannerId is not null)
        {
            filtered = filtered.Where(p =>
                (p.Planners ?? new List<string>()).Any(x =>
                    string.Equals(x, plannerId, StringComparison.OrdinalIgnoreCase)
                )
            );
        }

        var plans = filtered
            .Where(p => !string.IsNullOrWhiteSpace(p.Id))
            .OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var results = new List<PlanPlanningResult>(plans.Length);
        var totalCreated = 0;
        var totalUpdated = 0;

        foreach (var plan in plans)
        {
            var (resolvedId, created, updated) = await _reconciler.ReconcileAsync(
                plan.Id,
                request.DryRun,
                forum: null,
                cancellationToken: cancellationToken
            );

            results.Add(new PlanPlanningResult(resolvedId, created, updated));
            totalCreated += created;
            totalUpdated += updated;
        }

        return new PlanForestResult(
            PlanId: planId,
            PlannerId: plannerId,
            PlansPlanned: results.Count,
            PlantsCreated: totalCreated,
            PlantsUpdated: totalUpdated,
            Plans: results,
            DryRun: request.DryRun
        );
    }

    private static string? Normalize(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
