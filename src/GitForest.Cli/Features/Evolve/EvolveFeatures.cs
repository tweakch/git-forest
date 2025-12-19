using System.Text;
using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Mediator;

namespace GitForest.Cli.Features.Evolve;

public sealed record EvolvePlantCommand(
    string Selector,
    string PlanterId,
    string BranchOption,
    string Mode,
    bool DryRun
) : IRequest<EvolvePlantResult>;

public sealed record EvolvePlantResult(
    string PlantKey,
    string BranchName,
    string Status,
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

internal sealed class EvolvePlantHandler : IRequestHandler<EvolvePlantCommand, EvolvePlantResult>
{
    private readonly IPlantRepository _plants;

    public EvolvePlantHandler(IPlantRepository plants)
    {
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
    }

    public async Task<EvolvePlantResult> Handle(
        EvolvePlantCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var mode = NormalizeMode(request.Mode);
        var planterId = (request.PlanterId ?? string.Empty).Trim();
        if (planterId.Length == 0)
        {
            throw new ArgumentException("Planter ID must be provided.", nameof(request.PlanterId));
        }

        var resolved = await PlantSelector.ResolveAsync(
            _plants,
            request.Selector,
            cancellationToken
        );
        var updated = Clone(resolved);

        var branchName = ComputeBranchName(planterId, updated.Key, request.BranchOption);
        var planters = (updated.AssignedPlanters ?? new List<string>()).ToList();
        if (
            !planters.Any(p => string.Equals(p, planterId, StringComparison.OrdinalIgnoreCase))
        )
        {
            planters.Add(planterId);
        }

        var branches = (updated.Branches ?? new List<string>()).ToList();
        if (
            !branches.Any(b =>
                string.Equals(b, branchName, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            branches.Add(branchName);
        }

        var status = updated.Status ?? "planned";
        if (
            string.Equals(status, "planned", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "planted", StringComparison.OrdinalIgnoreCase)
        )
        {
            status = "growing";
        }

        updated.AssignedPlanters = planters;
        updated.Branches = branches;
        updated.Status = status;

        if (!request.DryRun)
        {
            updated.LastActivityDate = DateTime.UtcNow;
            await _plants.UpdateAsync(updated, cancellationToken);
        }

        return new EvolvePlantResult(
            PlantKey: updated.Key,
            BranchName: branchName,
            Status: updated.Status,
            Mode: mode,
            DryRun: request.DryRun
        );
    }

    private static string NormalizeMode(string mode)
    {
        var normalized = (mode ?? "propose").Trim();
        if (
            !string.Equals(normalized, "apply", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalized, "propose", StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new InvalidModeException(mode ?? string.Empty);
        }

        return normalized.ToLowerInvariant();
    }

    private static string ComputeBranchName(string planterId, string plantKey, string? branchOption)
    {
        var opt = (branchOption ?? "auto").Trim();
        if (!string.Equals(opt, "auto", StringComparison.OrdinalIgnoreCase) && opt.Length > 0)
        {
            return NormalizeBranchName(opt);
        }

        var (planId, slug) = ForestStore.SplitPlantKey(plantKey);
        return NormalizeBranchName($"{planterId}/{planId}__{slug}");
    }

    private static string NormalizeBranchName(string input)
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
