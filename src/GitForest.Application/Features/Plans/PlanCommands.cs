using GitForest.Core.Services;
using MediatR;

namespace GitForest.Application.Features.Plans;

public sealed record InstallPlanCommand(string Source) : IRequest<InstalledPlanResult>;

public sealed record InstalledPlanResult(string Id, string Version);

internal sealed class InstallPlanHandler : IRequestHandler<InstallPlanCommand, InstalledPlanResult>
{
    private readonly IPlanInstaller _installer;

    public InstallPlanHandler(IPlanInstaller installer)
    {
        _installer = installer ?? throw new ArgumentNullException(nameof(installer));
    }

    public async Task<InstalledPlanResult> Handle(
        InstallPlanCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        var (planId, version) = await _installer.InstallAsync(request.Source, cancellationToken);
        return new InstalledPlanResult(Id: planId, Version: version);
    }
}

public sealed record ReconcilePlanCommand(string PlanId, bool DryRun, string? Forum = null)
    : IRequest<ReconcileResult>;

public sealed record ReconcileResult(string PlanId, int PlantsCreated, int PlantsUpdated);

public sealed class PlanNotInstalledException : Exception
{
    public string PlanId { get; }

    public PlanNotInstalledException(string planId)
        : base($"Plan not installed: '{planId}'.")
    {
        PlanId = planId;
    }
}

internal sealed class ReconcilePlanHandler : IRequestHandler<ReconcilePlanCommand, ReconcileResult>
{
    private readonly IPlanReconciler _reconciler;

    public ReconcilePlanHandler(IPlanReconciler reconciler)
    {
        _reconciler = reconciler ?? throw new ArgumentNullException(nameof(reconciler));
    }

    public async Task<ReconcileResult> Handle(
        ReconcilePlanCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            var (planId, created, updated) = await _reconciler.ReconcileAsync(
                request.PlanId,
                request.DryRun,
                request.Forum,
                cancellationToken
            );
            return new ReconcileResult(
                PlanId: planId,
                PlantsCreated: created,
                PlantsUpdated: updated
            );
        }
        catch (DirectoryNotFoundException)
        {
            throw new PlanNotInstalledException(request.PlanId);
        }
    }
}
