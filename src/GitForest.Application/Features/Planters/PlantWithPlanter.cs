using GitForest.Application.Features.Plants;
using GitForest.Application.Features.Plants.Commands;
using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Core.Services;
using GitForest.Mediator;

namespace GitForest.Application.Features.Planters;

public sealed record PlantWithPlanterCommand(
    string PlanterId,
    string Selector,
    string BranchOption,
    bool Yes,
    bool DryRun
) : IRequest<PlantWithPlanterResult>;

public sealed record PlantWithPlanterResult(string PlantKey, string BranchName, bool DryRun);

public sealed class PlanterNotFoundException : Exception
{
    public string PlanterId { get; }

    public PlanterNotFoundException(string planterId)
        : base($"Planter not found: '{planterId}'.")
    {
        PlanterId = planterId;
    }
}

public sealed class ConfirmationRequiredException : Exception
{
    public string BranchName { get; }

    public ConfirmationRequiredException(string branchName)
        : base("Confirmation required.")
    {
        BranchName = branchName;
    }
}

internal sealed class PlantWithPlanterHandler
    : IRequestHandler<PlantWithPlanterCommand, PlantWithPlanterResult>
{
    private readonly IPlanRepository _plans;
    private readonly IPlantRepository _plants;
    private readonly IPlanterDiscovery _planterDiscovery;
    private readonly IGitService _git;

    public PlantWithPlanterHandler(
        IPlanRepository plans,
        IPlantRepository plants,
        IPlanterDiscovery planterDiscovery,
        IGitService git
    )
    {
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
        _planterDiscovery =
            planterDiscovery ?? throw new ArgumentNullException(nameof(planterDiscovery));
        _git = git ?? throw new ArgumentNullException(nameof(git));
    }

    public async Task<PlantWithPlanterResult> Handle(
        PlantWithPlanterCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var planterId = (request.PlanterId ?? string.Empty).Trim();
        if (planterId.Length == 0)
        {
            throw new PlanterNotFoundException(request.PlanterId ?? string.Empty);
        }

        var planterInfo = await GetPlanterInfoAsync(planterId, cancellationToken);
        if (!planterInfo.Exists)
        {
            throw new PlanterNotFoundException(planterId);
        }

        var plant = await PlantSelector.ResolveAsync(_plants, request.Selector, cancellationToken);
        var branchName = PlanterBranchNaming.ComputeBranchName(
            planterId,
            plant.Key,
            request.BranchOption
        );

        var updated = Clone(plant);
        ApplyPlanterAndBranch(updated, planterId, branchName);

        if (!request.DryRun)
        {
            updated.LastActivityDate = DateTime.UtcNow;
            await _plants.UpdateAsync(updated, cancellationToken);
        }

        // Preserve existing semantics: metadata is updated even if confirmation is missing; confirmation gates only git checkout.
        if (!request.DryRun && !request.Yes)
        {
            throw new ConfirmationRequiredException(branchName);
        }

        if (!request.DryRun)
        {
            var repoRoot = _git.GetRepoRoot();
            _git.CheckoutBranch(branchName, createIfMissing: true, workingDirectory: repoRoot);
        }

        return new PlantWithPlanterResult(
            PlantKey: updated.Key,
            BranchName: branchName,
            DryRun: request.DryRun
        );
    }

    private async Task<PlanterInfoResult> GetPlanterInfoAsync(
        string planterId,
        CancellationToken cancellationToken
    )
    {
        var info = await new GetPlanterHandler(_plans, _planterDiscovery).Handle(
            new GetPlanterQuery(planterId),
            cancellationToken
        );
        return info;
    }

    private static void ApplyPlanterAndBranch(Plant plant, string planterId, string branchName)
    {
        var planters = (plant.AssignedPlanters ?? new List<string>()).ToList();
        if (!planters.Any(x => string.Equals(x, planterId, StringComparison.OrdinalIgnoreCase)))
        {
            planters.Add(planterId);
        }
        plant.AssignedPlanters = planters;

        var branches = (plant.Branches ?? new List<string>()).ToList();
        if (!branches.Any(x => string.Equals(x, branchName, StringComparison.OrdinalIgnoreCase)))
        {
            branches.Add(branchName);
        }
        plant.Branches = branches;

        if (string.Equals(plant.Status, "planned", StringComparison.OrdinalIgnoreCase))
        {
            plant.Status = "planted";
        }
    }

    private static Plant Clone(Plant source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        return new Plant
        {
            Key = source.Key,
            Slug = source.Slug,
            PlanId = source.PlanId,
            PlannerId = source.PlannerId,
            Status = source.Status,
            Title = source.Title,
            Description = source.Description,
            AssignedPlanters = source.AssignedPlanters is null
                ? new List<string>()
                : new List<string>(source.AssignedPlanters),
            Branches = source.Branches is null
                ? new List<string>()
                : new List<string>(source.Branches),
            SelectedBranch = source.SelectedBranch,
            CreatedDate = source.CreatedDate,
            LastActivityDate = source.LastActivityDate,
        };
    }
}
