using System.Text;
using GitForest.Application.Features.Plans;
using GitForest.Application.Features.Plants;
using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Core.Services;
using GitForest.Core.Specifications.Plans;
using GitForest.Core.Specifications.Plants;
using GitForest.Mediator;

namespace GitForest.Application.Features.Evolve;

public sealed record EvolveForestCommand(string? PlanId, string? PlantSelector, bool DryRun)
    : IRequest<EvolveForestResult>;

public sealed record EvolveForestResult(
    string? PlanId,
    string? PlantKey,
    int PlantsConsidered,
    int PlantsEvolved,
    int Skipped,
    bool DryRun
);

internal static class EvolveHelpers
{
    public static string ComputeBaseBranchName(string planterId, string plantKey)
    {
        var id = (planterId ?? string.Empty).Trim();
        if (id.Length == 0)
        {
            return "git-forest/untitled";
        }

        var (planId, slug) = SplitPlantKey(plantKey);
        return NormalizeBranchName($"{id}/{planId}__{slug}");
    }

    public static (string planId, string slug) SplitPlantKey(string plantKey)
    {
        var key = (plantKey ?? string.Empty).Trim();
        if (key.Length == 0)
        {
            return (string.Empty, string.Empty);
        }

        var idx = key.IndexOf(':', StringComparison.Ordinal);
        if (idx <= 0 || idx == key.Length - 1)
        {
            return (key, string.Empty);
        }

        return (key[..idx], key[(idx + 1)..]);
    }

    public static string NormalizeBranchName(string input)
    {
        var trimmed = (input ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return "git-forest/untitled";
        }

        var sb = new StringBuilder(trimmed.Length);
        var lastWasDash = false;
        foreach (var ch in trimmed)
        {
            if (char.IsLetterOrDigit(ch) || ch is '/' or '-' or '_' or '.')
            {
                sb.Append(ch);
                lastWasDash = false;
                continue;
            }

            if (!lastWasDash)
            {
                sb.Append('-');
                lastWasDash = true;
            }
        }

        var normalized = sb.ToString().Trim('-');
        return normalized.Length == 0 ? "git-forest/untitled" : normalized;
    }

    public static Plant Clone(Plant source)
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

    public static bool IsEligibleForEvolve(Plant plant)
    {
        var status = (plant.Status ?? string.Empty).Trim();
        if (status.Length == 0)
        {
            return true;
        }

        return status.Equals("planned", StringComparison.OrdinalIgnoreCase)
            || status.Equals("planted", StringComparison.OrdinalIgnoreCase)
            || status.Equals("growing", StringComparison.OrdinalIgnoreCase)
            || status.Equals("harvestable", StringComparison.OrdinalIgnoreCase);
    }

    public static List<string> NormalizeOrderedIds(IEnumerable<string>? values)
    {
        var list = new List<string>();
        if (values is null)
        {
            return list;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in values)
        {
            var value = (raw ?? string.Empty).Trim();
            if (value.Length == 0 || !seen.Add(value))
            {
                continue;
            }

            list.Add(value);
        }

        return list;
    }

    public static bool ListContainsIgnoreCase(IReadOnlyList<string> values, string value)
    {
        var v = (value ?? string.Empty).Trim();
        if (v.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < values.Count; i++)
        {
            if (string.Equals(values[i], v, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static void ApplyEvolutionMetadata(Plant updated, string planterId, string branchName)
    {
        var planters = NormalizeOrderedIds(updated.AssignedPlanters);
        if (!planters.Any(p => string.Equals(p, planterId, StringComparison.OrdinalIgnoreCase)))
        {
            planters.Add(planterId);
        }

        var branches = NormalizeOrderedIds(updated.Branches);
        if (!branches.Any(b => string.Equals(b, branchName, StringComparison.OrdinalIgnoreCase)))
        {
            branches.Add(branchName);
        }

        var status = (updated.Status ?? "planned").Trim();
        if (
            status.Equals("planned", StringComparison.OrdinalIgnoreCase)
            || status.Equals("planted", StringComparison.OrdinalIgnoreCase)
        )
        {
            status = "growing";
        }

        updated.AssignedPlanters = planters;
        updated.Branches = branches;
        updated.Status = status;
    }
}

internal sealed class EvolveForestHandler : IRequestHandler<EvolveForestCommand, EvolveForestResult>
{
    private readonly IPlanRepository _plans;
    private readonly IPlantRepository _plants;
    private readonly IPlanReconciler _reconciler;
    private readonly IGitService _git;

    public EvolveForestHandler(
        IPlanRepository plans,
        IPlantRepository plants,
        IPlanReconciler reconciler,
        IGitService git
    )
    {
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
        _reconciler = reconciler ?? throw new ArgumentNullException(nameof(reconciler));
        _git = git ?? throw new ArgumentNullException(nameof(git));
    }

    public async Task<EvolveForestResult> Handle(
        EvolveForestCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var planId = Normalize(request.PlanId);
        var plantSelector = Normalize(request.PlantSelector);

        // Single-plant mode: no lazy seeding (matches spec).
        if (plantSelector is not null)
        {
            var resolved = await PlantSelector.ResolveAsync(
                _plants,
                plantSelector,
                cancellationToken
            );
            var (considered, evolved, skipped) = await EvolveOnePlantAsync(
                resolved,
                request.DryRun,
                cancellationToken
            );

            return new EvolveForestResult(
                PlanId: null,
                PlantKey: resolved.Key,
                PlantsConsidered: considered,
                PlantsEvolved: evolved,
                Skipped: skipped,
                DryRun: request.DryRun
            );
        }

        // Validate plan existence if scoped.
        var allPlans = await _plans.ListAsync(new AllPlansSpec(), cancellationToken);
        if (planId is not null)
        {
            var exists = allPlans.Any(p =>
                string.Equals(p.Id, planId, StringComparison.OrdinalIgnoreCase)
            );
            if (!exists)
            {
                throw new PlanNotInstalledException(planId);
            }
        }

        var plansToConsider = planId is null
            ? allPlans
                .Where(p => !string.IsNullOrWhiteSpace(p.Id))
                .OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : allPlans
                .Where(p => string.Equals(p.Id, planId, StringComparison.OrdinalIgnoreCase))
                .ToArray();

        // Lazy seed: for any plan with zero plants, run plan reconciliation.
        foreach (var plan in plansToConsider)
        {
            var id = (plan.Id ?? string.Empty).Trim();
            if (id.Length == 0)
            {
                continue;
            }

            var existing = await _plants.ListAsync(new PlantsByPlanIdSpec(id), cancellationToken);
            if (existing.Count == 0)
            {
                _ = await _reconciler.ReconcileAsync(
                    id,
                    request.DryRun,
                    forum: null,
                    cancellationToken: cancellationToken
                );
            }
        }

        var plantsInScope = planId is null
            ? await _plants.ListAsync(new AllPlantsSpec(), cancellationToken)
            : await _plants.ListAsync(new PlantsByPlanIdSpec(planId), cancellationToken);

        var consideredTotal = 0;
        var evolvedTotal = 0;
        var skippedTotal = 0;

        foreach (var plant in plantsInScope)
        {
            if (plant is null || string.IsNullOrWhiteSpace(plant.Key))
            {
                continue;
            }

            var (considered, evolved, skipped) = await EvolveOnePlantAsync(
                plant,
                request.DryRun,
                cancellationToken
            );
            consideredTotal += considered;
            evolvedTotal += evolved;
            skippedTotal += skipped;
        }

        return new EvolveForestResult(
            PlanId: planId,
            PlantKey: null,
            PlantsConsidered: consideredTotal,
            PlantsEvolved: evolvedTotal,
            Skipped: skippedTotal,
            DryRun: request.DryRun
        );
    }

    private async Task<(int considered, int evolved, int skipped)> EvolveOnePlantAsync(
        Plant plant,
        bool dryRun,
        CancellationToken cancellationToken
    )
    {
        var considered = 1;

        if (!EvolveHelpers.IsEligibleForEvolve(plant))
        {
            return (considered, evolved: 0, skipped: 1);
        }

        var updated = EvolveHelpers.Clone(plant);

        var plantKey = updated.Key ?? string.Empty;
        var (planId, _) = EvolveHelpers.SplitPlantKey(plantKey);

        var planters = EvolveHelpers.NormalizeOrderedIds(updated.AssignedPlanters);
        if (planters.Count == 0)
        {
            var plan = await _plans.GetBySpecAsync(new PlanByIdSpec(planId), cancellationToken);
            if (plan is not null)
            {
                planters = EvolveHelpers.NormalizeOrderedIds(plan.Planters);
            }
        }

        if (planters.Count == 0)
        {
            return (considered, evolved: 0, skipped: 1);
        }

        var existingBranches = EvolveHelpers.NormalizeOrderedIds(updated.Branches);
        var evolvedThisPlant = false;
        foreach (var planterId in planters)
        {
            var baseBranch = EvolveHelpers.ComputeBaseBranchName(planterId, plantKey);

            var chosenBranch = ChooseAvailableBranchName(_git, baseBranch, existingBranches);

            if (!dryRun)
            {
                _git.CreateBranch(chosenBranch);
            }

            EvolveHelpers.ApplyEvolutionMetadata(updated, planterId, chosenBranch);
            existingBranches = EvolveHelpers.NormalizeOrderedIds(updated.Branches);
            evolvedThisPlant = true;
        }

        if (evolvedThisPlant && !dryRun)
        {
            updated.LastActivityDate = DateTime.UtcNow;
            await _plants.UpdateAsync(updated, cancellationToken);
        }

        return evolvedThisPlant
            ? (considered, evolved: 1, skipped: 0)
            : (considered, evolved: 0, skipped: 1);
    }

    private static string? Normalize(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static string ChooseAvailableBranchName(
        IGitService git,
        string baseBranchName,
        IReadOnlyList<string> existingBranches
    )
    {
        var baseName = (baseBranchName ?? string.Empty).Trim();
        if (baseName.Length == 0)
        {
            baseName = "git-forest/untitled";
        }

        if (
            !git.BranchExists(baseName)
            && !EvolveHelpers.ListContainsIgnoreCase(existingBranches, baseName)
        )
        {
            return baseName;
        }

        for (var suffix = 2; suffix < 10000; suffix++)
        {
            var candidate = $"{baseName}--{suffix}";
            if (git.BranchExists(candidate))
            {
                continue;
            }

            if (EvolveHelpers.ListContainsIgnoreCase(existingBranches, candidate))
            {
                continue;
            }

            return candidate;
        }

        throw new InvalidOperationException(
            $"Unable to find available branch name for base '{baseName}'."
        );
    }
}
