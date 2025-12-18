using System.Text;
using GitForest.Infrastructure.FileSystem.Serialization;
using MediatR;

namespace GitForest.Cli.Features.Planters;

public sealed record ListPlantersQuery(bool IncludeBuiltin, bool IncludeCustom)
    : IRequest<IReadOnlyList<PlanterRow>>;

public sealed record PlanterRow(string Id, string Kind, string[] Plans);

internal sealed class ListPlantersHandler
    : IRequestHandler<ListPlantersQuery, IReadOnlyList<PlanterRow>>
{
    public Task<IReadOnlyList<PlanterRow>> Handle(
        ListPlantersQuery request,
        CancellationToken cancellationToken
    )
    {
        _ = cancellationToken;

        var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);

        var rows = new List<PlanterRow>();

        if (request.IncludeBuiltin)
        {
            var plans = ForestStore.ListPlans(forestDir);

            // Aggregate unique planters across installed plans, also tracking which plan(s) reference each planter.
            var planters = new Dictionary<string, HashSet<string>>(
                StringComparer.OrdinalIgnoreCase
            );
            foreach (var installed in plans)
            {
                if (string.IsNullOrWhiteSpace(installed.Id))
                {
                    continue;
                }

                var planId = installed.Id.Trim();
                var planYamlPath = Path.Combine(forestDir, "plans", planId, "plan.yaml");
                if (!File.Exists(planYamlPath))
                {
                    continue;
                }

                try
                {
                    var yaml = File.ReadAllText(planYamlPath);
                    var parsed = PlanYamlLite.Parse(yaml);
                    foreach (var rawPlanterId in parsed.Planters ?? Array.Empty<string>())
                    {
                        if (string.IsNullOrWhiteSpace(rawPlanterId))
                        {
                            continue;
                        }

                        var planterId = rawPlanterId.Trim();
                        if (!planters.TryGetValue(planterId, out var referencedByPlans))
                        {
                            referencedByPlans = new HashSet<string>(
                                StringComparer.OrdinalIgnoreCase
                            );
                            planters[planterId] = referencedByPlans;
                        }

                        referencedByPlans.Add(planId);
                    }
                }
                catch
                {
                    // best-effort: ignore invalid plan YAML
                }
            }

            rows.AddRange(
                planters
                    .Select(kvp => new PlanterRow(
                        Id: kvp.Key,
                        Kind: "builtin",
                        Plans: kvp.Value.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()
                    ))
                    .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            );
        }

        if (request.IncludeCustom)
        {
            // Best-effort: any directory names under .git-forest/planters are treated as custom planters.
            var plantersDir = Path.Combine(forestDir, "planters");
            if (Directory.Exists(plantersDir))
            {
                foreach (var dir in Directory.GetDirectories(plantersDir))
                {
                    var id = Path.GetFileName(dir);
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        rows.Add(
                            new PlanterRow(
                                Id: id.Trim(),
                                Kind: "custom",
                                Plans: Array.Empty<string>()
                            )
                        );
                    }
                }
            }
        }

        // De-duplicate: if a custom planter shares the same id as a builtin, keep builtin.
        var merged = rows.GroupBy(r => r.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var builtinRow = g.FirstOrDefault(x =>
                    string.Equals(x.Kind, "builtin", StringComparison.OrdinalIgnoreCase)
                );
                return builtinRow ?? g.First();
            })
            .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult((IReadOnlyList<PlanterRow>)merged);
    }
}

public sealed record GetPlanterQuery(string PlanterId) : IRequest<PlanterInfoResult>;

public sealed record PlanterInfoResult(bool Exists, string Id, string Kind, string[] Plans);

internal sealed class GetPlanterHandler : IRequestHandler<GetPlanterQuery, PlanterInfoResult>
{
    public Task<PlanterInfoResult> Handle(
        GetPlanterQuery request,
        CancellationToken cancellationToken
    )
    {
        _ = cancellationToken;
        var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
        var info = GetPlanterInfo(forestDir, request.PlanterId);
        return Task.FromResult(new PlanterInfoResult(info.Exists, info.Id, info.Kind, info.Plans));
    }

    internal static (bool Exists, string Id, string Kind, string[] Plans) GetPlanterInfo(
        string forestDir,
        string planterId
    )
    {
        var id = (planterId ?? string.Empty).Trim();
        if (id.Length == 0)
        {
            return (false, string.Empty, string.Empty, Array.Empty<string>());
        }

        var plans = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var installedPlans = ForestStore.ListPlans(forestDir);
        foreach (var installed in installedPlans)
        {
            if (string.IsNullOrWhiteSpace(installed.Id))
            {
                continue;
            }

            var planId = installed.Id.Trim();
            var planYamlPath = Path.Combine(forestDir, "plans", planId, "plan.yaml");
            if (!File.Exists(planYamlPath))
            {
                continue;
            }

            try
            {
                var yaml = File.ReadAllText(planYamlPath);
                var parsed = PlanYamlLite.Parse(yaml);
                foreach (var raw in parsed.Planters ?? Array.Empty<string>())
                {
                    if (
                        string.Equals(
                            (raw ?? string.Empty).Trim(),
                            id,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        plans.Add(planId);
                    }
                }
            }
            catch
            {
                // best-effort
            }
        }

        var isCustom = Directory.Exists(Path.Combine(forestDir, "planters", id));
        var isBuiltin = plans.Count > 0;
        var exists = isBuiltin || isCustom;
        var kind = isBuiltin ? "builtin" : (isCustom ? "custom" : string.Empty);
        return (
            exists,
            id,
            kind,
            plans.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()
        );
    }
}

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
    public Task<PlantWithPlanterResult> Handle(
        PlantWithPlanterCommand request,
        CancellationToken cancellationToken
    )
    {
        _ = cancellationToken;

        var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);

        var info = GetPlanterHandler.GetPlanterInfo(forestDir, request.PlanterId);
        if (!info.Exists)
        {
            throw new PlanterNotFoundException(request.PlanterId);
        }

        var plant = ForestStore.ResolvePlant(forestDir, request.Selector);
        var branchName = ComputeBranchName(request.PlanterId, plant.Key, request.BranchOption);

        var updated = ForestStore.UpdatePlant(
            forestDir,
            request.Selector,
            p =>
            {
                var planters = (p.AssignedPlanters ?? Array.Empty<string>()).ToList();
                if (
                    !planters.Any(x =>
                        string.Equals(x, request.PlanterId, StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    planters.Add(request.PlanterId);
                }

                var branches = (p.Branches ?? Array.Empty<string>()).ToList();
                if (
                    !branches.Any(x =>
                        string.Equals(x, branchName, StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    branches.Add(branchName);
                }

                var status = p.Status;
                if (string.Equals(status, "planned", StringComparison.OrdinalIgnoreCase))
                {
                    status = "planted";
                }

                return p with
                {
                    AssignedPlanters = planters,
                    Branches = branches,
                    Status = status,
                };
            },
            request.DryRun
        );

        if (!request.DryRun)
        {
            if (!request.Yes)
            {
                throw new ConfirmationRequiredException(branchName);
            }

            var repoRoot = GitRunner.GetRepoRoot();
            GitRunner.CheckoutBranch(branchName, createIfMissing: true, workingDirectory: repoRoot);
        }

        return Task.FromResult(
            new PlantWithPlanterResult(
                PlantKey: updated.Key,
                BranchName: branchName,
                DryRun: request.DryRun
            )
        );
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
}

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
    public Task<GrowWithPlanterResult> Handle(
        GrowWithPlanterCommand request,
        CancellationToken cancellationToken
    )
    {
        _ = cancellationToken;

        var mode = (request.Mode ?? "apply").Trim();
        if (
            !string.Equals(mode, "apply", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(mode, "propose", StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new InvalidModeException(mode);
        }

        var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);

        var info = GetPlanterHandler.GetPlanterInfo(forestDir, request.PlanterId);
        if (!info.Exists)
        {
            throw new PlanterNotFoundException(request.PlanterId);
        }

        var plant = ForestStore.ResolvePlant(forestDir, request.Selector);
        var branchName = plant.Branches.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(branchName))
        {
            branchName = ComputeBranchName(request.PlanterId, plant.Key, "auto");
        }

        // Mark growing (apply mode only) before we attempt the work.
        if (!request.DryRun && string.Equals(mode, "apply", StringComparison.OrdinalIgnoreCase))
        {
            _ = ForestStore.UpdatePlant(
                forestDir,
                request.Selector,
                p =>
                {
                    var planters = (p.AssignedPlanters ?? Array.Empty<string>()).ToList();
                    if (
                        !planters.Any(x =>
                            string.Equals(x, request.PlanterId, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                    {
                        planters.Add(request.PlanterId);
                    }

                    var branches = (p.Branches ?? Array.Empty<string>()).ToList();
                    if (
                        !branches.Any(x =>
                            string.Equals(x, branchName, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                    {
                        branches.Add(branchName);
                    }

                    return p with
                    {
                        AssignedPlanters = planters,
                        Branches = branches,
                        Status = "growing",
                    };
                },
                dryRun: false
            );
        }

        if (string.Equals(mode, "apply", StringComparison.OrdinalIgnoreCase))
        {
            if (!request.DryRun)
            {
                var repoRoot = GitRunner.GetRepoRoot();
                GitRunner.CheckoutBranch(
                    branchName,
                    createIfMissing: true,
                    workingDirectory: repoRoot
                );

                ApplyDeterministicGrowth(
                    repoRoot,
                    plantKey: plant.Key,
                    planterId: request.PlanterId
                );

                if (GitRunner.HasUncommittedChanges(repoRoot))
                {
                    GitRunner.AddAll(repoRoot);
                    GitRunner.Commit($"git-forest: grow {plant.Key}", repoRoot);
                }
            }
        }

        var final = ForestStore.UpdatePlant(
            forestDir,
            request.Selector,
            p =>
            {
                var planters = (p.AssignedPlanters ?? Array.Empty<string>()).ToList();
                if (
                    !planters.Any(x =>
                        string.Equals(x, request.PlanterId, StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    planters.Add(request.PlanterId);
                }

                var branches = (p.Branches ?? Array.Empty<string>()).ToList();
                if (
                    !branches.Any(x =>
                        string.Equals(x, branchName, StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    branches.Add(branchName);
                }

                return p with
                {
                    AssignedPlanters = planters,
                    Branches = branches,
                    Status = "harvestable",
                };
            },
            request.DryRun
        );

        return Task.FromResult(
            new GrowWithPlanterResult(
                PlantKey: final.Key,
                BranchName: branchName,
                Mode: mode,
                DryRun: request.DryRun
            )
        );
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

    private static void ApplyDeterministicGrowth(string repoRoot, string plantKey, string planterId)
    {
        // MVP: a deterministic, safe change that works in any repo. Keep it small and idempotent.
        var readmePath = Path.Combine(repoRoot, "README.md");
        if (!File.Exists(readmePath))
        {
            File.WriteAllText(readmePath, "# Repository\n", Encoding.UTF8);
        }

        var marker = $"<!-- git-forest: {plantKey} (planter={planterId}) -->";
        var content = File.ReadAllText(readmePath, Encoding.UTF8);
        if (content.Contains(marker, StringComparison.Ordinal))
        {
            return;
        }

        var updated =
            content.TrimEnd()
            + Environment.NewLine
            + Environment.NewLine
            + marker
            + Environment.NewLine;
        File.WriteAllText(readmePath, updated, Encoding.UTF8);
    }
}
