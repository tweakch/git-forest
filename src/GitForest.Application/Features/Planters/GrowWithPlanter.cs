using GitForest.Application.Features.Plants;
using GitForest.Application.Features.Plants.Commands;
using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Core.Services;
using GitForest.Mediator;

namespace GitForest.Application.Features.Planters;

public sealed record GrowWithPlanterCommand(
    string PlanterId,
    string Selector,
    string Mode,
    bool DryRun
) : IRequest<GrowWithPlanterResult>;

public sealed record GrowWithPlanterResult(
    string PlantKey,
    string BranchName,
    string Mode,
    bool DryRun
);

public sealed class InvalidModeException : Exception
{
    public string Mode { get; }

    public InvalidModeException(string mode)
        : base("Invalid --mode. Expected: propose|apply")
    {
        Mode = mode;
    }
}

internal sealed class GrowWithPlanterHandler
    : IRequestHandler<GrowWithPlanterCommand, GrowWithPlanterResult>
{
    private readonly IPlanRepository _plans;
    private readonly IPlantRepository _plants;
    private readonly IPlanterDiscovery _planterDiscovery;
    private readonly IGitService _git;
    private readonly IPlanterGrowthApplier _growth;

    public GrowWithPlanterHandler(
        IPlanRepository plans,
        IPlantRepository plants,
        IPlanterDiscovery planterDiscovery,
        IGitService git,
        IPlanterGrowthApplier growth
    )
    {
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
        _planterDiscovery =
            planterDiscovery ?? throw new ArgumentNullException(nameof(planterDiscovery));
        _git = git ?? throw new ArgumentNullException(nameof(git));
        _growth = growth ?? throw new ArgumentNullException(nameof(growth));
    }

    public async Task<GrowWithPlanterResult> Handle(
        GrowWithPlanterCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var mode = (request.Mode ?? "apply").Trim();
        if (
            !string.Equals(mode, "apply", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(mode, "propose", StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new InvalidModeException(mode);
        }

        var planterId = (request.PlanterId ?? string.Empty).Trim();
        if (planterId.Length == 0)
        {
            throw new PlanterNotFoundException(request.PlanterId ?? string.Empty);
        }

        var planterInfo = await new GetPlanterHandler(_plans, _planterDiscovery).Handle(
            new GetPlanterQuery(planterId),
            cancellationToken
        );
        if (!planterInfo.Exists)
        {
            throw new PlanterNotFoundException(planterId);
        }

        var plant = await PlantSelector.ResolveAsync(_plants, request.Selector, cancellationToken);
        var branchName = plant.Branches.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(branchName))
        {
            branchName = PlanterBranchNaming.ComputeBranchName(planterId, plant.Key, "auto");
        }

        // Mark growing (apply mode only) before we attempt the work.
        if (!request.DryRun && string.Equals(mode, "apply", StringComparison.OrdinalIgnoreCase))
        {
            var growing = Clone(plant);
            ApplyPlanterAndBranch(growing, planterId, branchName);
            growing.Status = "growing";
            growing.LastActivityDate = DateTime.UtcNow;
            await _plants.UpdateAsync(growing, cancellationToken);
        }

        if (string.Equals(mode, "apply", StringComparison.OrdinalIgnoreCase) && !request.DryRun)
        {
            var repoRoot = _git.GetRepoRoot();
            _git.CheckoutBranch(branchName, createIfMissing: true, workingDirectory: repoRoot);

            _growth.ApplyDeterministicGrowth(repoRoot, plantKey: plant.Key, planterId: planterId);

            if (_git.HasUncommittedChanges(repoRoot))
            {
                _git.AddAll(repoRoot);
                _git.Commit($"git-forest: grow {plant.Key}", repoRoot);
            }
        }

        var final = Clone(plant);
        ApplyPlanterAndBranch(final, planterId, branchName);
        final.Status = "harvestable";

        if (!request.DryRun)
        {
            final.LastActivityDate = DateTime.UtcNow;
            await _plants.UpdateAsync(final, cancellationToken);
        }

        return new GrowWithPlanterResult(
            PlantKey: final.Key,
            BranchName: branchName,
            Mode: mode,
            DryRun: request.DryRun
        );
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
