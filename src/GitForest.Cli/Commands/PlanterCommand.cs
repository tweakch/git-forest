using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;

namespace GitForest.Cli.Commands;

public static class PlanterCommand
{
    public static Command Build(CliOptions cliOptions)
    {
        var planterCommand = new Command("planter", "Manage a specific planter");
        var planterIdArg = new Argument<string>("planter-id", "Planter identifier");
        planterCommand.AddArgument(planterIdArg);

        var showCommand = new Command("show", "Show planter details");
        showCommand.SetHandler((InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var planterId = context.ParseResult.GetValueForArgument(planterIdArg);
            var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);

            try
            {
                var info = GetPlanterInfo(forestDir, planterId);
                if (!info.Exists)
                {
                    if (output.Json)
                    {
                        output.WriteJsonError(code: "planter_not_found", message: "Planter not found", details: new { planterId });
                    }
                    else
                    {
                        output.WriteErrorLine($"Planter '{planterId}': not found");
                    }

                    context.ExitCode = ExitCodes.PlanterNotFound;
                    return;
                }

                if (output.Json)
                {
                    output.WriteJson(new { planter = new { id = info.Id, kind = info.Kind, plans = info.Plans } });
                }
                else
                {
                    var plansText = info.Plans.Length == 0 ? "-" : string.Join(", ", info.Plans);
                    output.WriteLine($"Id: {info.Id}");
                    output.WriteLine($"Kind: {info.Kind}");
                    output.WriteLine($"Plans: {plansText}");
                }

                context.ExitCode = ExitCodes.Success;
            }
            catch (ForestStore.ForestNotInitializedException)
            {
                // For single-planter lookup, treat missing forest as "not found" (keeps CLI usable in fresh repos
                // and matches the smoke-test contract).
                if (output.Json)
                {
                    output.WriteJsonError(code: "planter_not_found", message: "Planter not found", details: new { planterId });
                }
                else
                {
                    output.WriteErrorLine($"Planter '{planterId}': not found");
                }

                context.ExitCode = ExitCodes.PlanterNotFound;
            }
        });

        var plantCommand = new Command("plant", "Assign + create branch for a plant");
        var selectorArg = new Argument<string>("selector", "Plant selector (key, slug, or P01)");
        var branchOption = new Option<string>("--branch", () => "auto", "Branch name to use ('auto' uses a deterministic default)");
        var yesOption = new Option<bool>("--yes", "Proceed without prompting");
        var dryRunOption = new Option<bool>("--dry-run", "Show what would be done without applying");
        plantCommand.AddArgument(selectorArg);
        plantCommand.AddOption(branchOption);
        plantCommand.AddOption(yesOption);
        plantCommand.AddOption(dryRunOption);
        plantCommand.SetHandler((InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var planterId = context.ParseResult.GetValueForArgument(planterIdArg);
            var selector = context.ParseResult.GetValueForArgument(selectorArg);
            var branch = context.ParseResult.GetValueForOption(branchOption);
            var yes = context.ParseResult.GetValueForOption(yesOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);

            var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);

            try
            {
                var info = GetPlanterInfo(forestDir, planterId);
                if (!info.Exists)
                {
                    if (output.Json)
                    {
                        output.WriteJsonError(code: "planter_not_found", message: "Planter not found", details: new { planterId });
                    }
                    else
                    {
                        output.WriteErrorLine($"Planter '{planterId}': not found");
                    }

                    context.ExitCode = ExitCodes.PlanterNotFound;
                    return;
                }

                var plant = ForestStore.ResolvePlant(forestDir, selector);
                var branchName = ComputeBranchName(planterId, plant.Key, branch);

                var updated = ForestStore.UpdatePlant(forestDir, selector, p =>
                {
                    var planters = (p.AssignedPlanters ?? Array.Empty<string>()).ToList();
                    if (!planters.Any(x => string.Equals(x, planterId, StringComparison.OrdinalIgnoreCase)))
                    {
                        planters.Add(planterId);
                    }

                    var branches = (p.Branches ?? Array.Empty<string>()).ToList();
                    if (!branches.Any(x => string.Equals(x, branchName, StringComparison.OrdinalIgnoreCase)))
                    {
                        branches.Add(branchName);
                    }

                    var status = p.Status;
                    if (string.Equals(status, "planned", StringComparison.OrdinalIgnoreCase))
                    {
                        status = "planted";
                    }

                    return p with { AssignedPlanters = planters, Branches = branches, Status = status };
                }, dryRun);

                if (!dryRun)
                {
                    if (!yes)
                    {
                        if (output.Json)
                        {
                            output.WriteJsonError(code: "confirmation_required", message: "Use --yes to create/check out a git branch.", details: new { branch = branchName });
                        }
                        else
                        {
                            output.WriteErrorLine("Error: confirmation required. Re-run with --yes.");
                        }

                        context.ExitCode = ExitCodes.InvalidArguments;
                        return;
                    }

                    var repoRoot = GitRunner.GetRepoRoot();
                    GitRunner.CheckoutBranch(branchName, createIfMissing: true, workingDirectory: repoRoot);
                }

                if (output.Json)
                {
                    output.WriteJson(new { status = "planted", dryRun, planterId, plantKey = updated.Key, branch = branchName });
                }
                else
                {
                    output.WriteLine(dryRun
                        ? $"Would plant '{updated.Key}' with planter '{planterId}' on branch '{branchName}'"
                        : $"Planted '{updated.Key}' with planter '{planterId}' on branch '{branchName}'");
                }

                context.ExitCode = ExitCodes.Success;
            }
            catch (ForestStore.ForestNotInitializedException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "forest_not_initialized", message: "Forest not initialized");
                }
                else
                {
                    output.WriteErrorLine("Error: forest not initialized");
                }

                context.ExitCode = ExitCodes.ForestNotInitialized;
            }
            catch (ForestStore.PlantNotFoundException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "plant_not_found", message: "Plant not found", details: new { selector });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{selector}': not found");
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
            catch (ForestStore.PlantAmbiguousSelectorException ex)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "plant_ambiguous", message: "Plant selector is ambiguous", details: new { selector = ex.Selector, matches = ex.Matches });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{ex.Selector}': ambiguous; matched {ex.Matches.Length} plants");
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
            catch (GitRunner.GitRunnerException ex)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "git_failed", message: ex.Message, details: new { exitCode = ex.ExitCode, stdout = ex.StdOut, stderr = ex.StdErr });
                }
                else
                {
                    output.WriteErrorLine($"Error: {ex.Message}");
                    if (!string.IsNullOrWhiteSpace(ex.StdErr))
                    {
                        output.WriteErrorLine(ex.StdErr.Trim());
                    }
                }

                context.ExitCode = ExitCodes.GitOperationFailed;
            }
        });

        var growCommand = new Command("grow", "Grow a plant (propose or apply changes)");
        var modeOption = new Option<string>("--mode", () => "apply", "Mode: propose|apply");
        growCommand.AddArgument(selectorArg);
        growCommand.AddOption(modeOption);
        growCommand.AddOption(dryRunOption);
        growCommand.SetHandler((InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var planterId = context.ParseResult.GetValueForArgument(planterIdArg);
            var selector = context.ParseResult.GetValueForArgument(selectorArg);
            var mode = (context.ParseResult.GetValueForOption(modeOption) ?? "apply").Trim();
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);

            try
            {
                var info = GetPlanterInfo(forestDir, planterId);
                if (!info.Exists)
                {
                    if (output.Json)
                    {
                        output.WriteJsonError(code: "planter_not_found", message: "Planter not found", details: new { planterId });
                    }
                    else
                    {
                        output.WriteErrorLine($"Planter '{planterId}': not found");
                    }

                    context.ExitCode = ExitCodes.PlanterNotFound;
                    return;
                }

                if (!string.Equals(mode, "apply", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(mode, "propose", StringComparison.OrdinalIgnoreCase))
                {
                    if (output.Json)
                    {
                        output.WriteJsonError(code: "invalid_arguments", message: "Invalid --mode. Expected: propose|apply", details: new { mode });
                    }
                    else
                    {
                        output.WriteErrorLine("Error: invalid --mode. Expected: propose|apply");
                    }

                    context.ExitCode = ExitCodes.InvalidArguments;
                    return;
                }

                var plant = ForestStore.ResolvePlant(forestDir, selector);
                var branchName = plant.Branches.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(branchName))
                {
                    branchName = ComputeBranchName(planterId, plant.Key, "auto");
                }

                // Mark growing (apply mode only) before we attempt the work.
                if (!dryRun && string.Equals(mode, "apply", StringComparison.OrdinalIgnoreCase))
                {
                    _ = ForestStore.UpdatePlant(forestDir, selector, p =>
                    {
                        var planters = (p.AssignedPlanters ?? Array.Empty<string>()).ToList();
                        if (!planters.Any(x => string.Equals(x, planterId, StringComparison.OrdinalIgnoreCase)))
                        {
                            planters.Add(planterId);
                        }

                        var branches = (p.Branches ?? Array.Empty<string>()).ToList();
                        if (!branches.Any(x => string.Equals(x, branchName, StringComparison.OrdinalIgnoreCase)))
                        {
                            branches.Add(branchName);
                        }

                        return p with { AssignedPlanters = planters, Branches = branches, Status = "growing" };
                    }, dryRun: false);
                }

                if (string.Equals(mode, "apply", StringComparison.OrdinalIgnoreCase))
                {
                    if (!dryRun)
                    {
                        var repoRoot = GitRunner.GetRepoRoot();
                        GitRunner.CheckoutBranch(branchName, createIfMissing: true, workingDirectory: repoRoot);

                        ApplyDeterministicGrowth(repoRoot, plantKey: plant.Key, planterId: planterId);

                        if (GitRunner.HasUncommittedChanges(repoRoot))
                        {
                            GitRunner.AddAll(repoRoot);
                            GitRunner.Commit($"git-forest: grow {plant.Key}", repoRoot);
                        }
                    }
                }

                var final = ForestStore.UpdatePlant(forestDir, selector, p =>
                {
                    var planters = (p.AssignedPlanters ?? Array.Empty<string>()).ToList();
                    if (!planters.Any(x => string.Equals(x, planterId, StringComparison.OrdinalIgnoreCase)))
                    {
                        planters.Add(planterId);
                    }

                    var branches = (p.Branches ?? Array.Empty<string>()).ToList();
                    if (!branches.Any(x => string.Equals(x, branchName, StringComparison.OrdinalIgnoreCase)))
                    {
                        branches.Add(branchName);
                    }

                    return p with
                    {
                        AssignedPlanters = planters,
                        Branches = branches,
                        Status = "harvestable"
                    };
                }, dryRun);

                if (output.Json)
                {
                    output.WriteJson(new { status = "harvestable", dryRun, mode, planterId, plantKey = final.Key, branch = branchName });
                }
                else
                {
                    output.WriteLine(dryRun
                        ? $"Would grow '{final.Key}' (mode={mode})"
                        : $"Grew '{final.Key}' (mode={mode}) → harvestable");
                }

                context.ExitCode = ExitCodes.Success;
            }
            catch (ForestStore.ForestNotInitializedException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "forest_not_initialized", message: "Forest not initialized");
                }
                else
                {
                    output.WriteErrorLine("Error: forest not initialized");
                }

                context.ExitCode = ExitCodes.ForestNotInitialized;
            }
            catch (ForestStore.PlantNotFoundException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "plant_not_found", message: "Plant not found", details: new { selector });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{selector}': not found");
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
            catch (ForestStore.PlantAmbiguousSelectorException ex)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "plant_ambiguous", message: "Plant selector is ambiguous", details: new { selector = ex.Selector, matches = ex.Matches });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{ex.Selector}': ambiguous; matched {ex.Matches.Length} plants");
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
            catch (GitRunner.GitRunnerException ex)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "git_failed", message: ex.Message, details: new { exitCode = ex.ExitCode, stdout = ex.StdOut, stderr = ex.StdErr });
                }
                else
                {
                    output.WriteErrorLine($"Error: {ex.Message}");
                    if (!string.IsNullOrWhiteSpace(ex.StdErr))
                    {
                        output.WriteErrorLine(ex.StdErr.Trim());
                    }
                }

                context.ExitCode = ExitCodes.GitOperationFailed;
            }
        });

        planterCommand.AddCommand(showCommand);
        planterCommand.AddCommand(plantCommand);
        planterCommand.AddCommand(growCommand);
        return planterCommand;
    }

    private static (bool Exists, string Id, string Kind, string[] Plans) GetPlanterInfo(string forestDir, string planterId)
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
                    if (string.Equals((raw ?? string.Empty).Trim(), id, StringComparison.OrdinalIgnoreCase))
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
        return (exists, id, kind, plans.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray());
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

        var updated = content.TrimEnd() + Environment.NewLine + Environment.NewLine + marker + Environment.NewLine;
        File.WriteAllText(readmePath, updated, Encoding.UTF8);
    }
}
