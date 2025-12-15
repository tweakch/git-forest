using System.CommandLine;
using System.CommandLine.Invocation;

namespace GitForest.Cli.Commands;

public static class PlansCommand
{
    public static Command Build(CliOptions cliOptions)
    {
        var plansCommand = new Command("plans", "Manage plans");

        var listCommand = new Command("list", "List installed plans");
        listCommand.SetHandler((InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);

            try
            {
                var plans = ForestStore.ListPlans(forestDir);
                if (output.Json)
                {
                    output.WriteJson(new
                    {
                        plans = plans.Select(p => new { id = p.Id, name = p.Name, version = p.Version, source = p.Source }).ToArray()
                    });
                }
                else
                {
                    if (plans.Count == 0)
                    {
                        output.WriteLine("No plans installed");
                    }
                    else
                    {
                        foreach (var plan in plans)
                        {
                            var version = string.IsNullOrWhiteSpace(plan.Version) ? "-" : plan.Version;
                            var name = string.IsNullOrWhiteSpace(plan.Name) ? "" : $"  {plan.Name}";
                            output.WriteLine($"{plan.Id}@{version}{name}");
                        }
                    }
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
        });
        plansCommand.AddCommand(listCommand);

        var installCommand = new Command("install", "Install a plan");
        var sourceArg = new Argument<string>("source", "Plan source (GitHub slug, URL, or local path)");
        installCommand.AddArgument(sourceArg);
        installCommand.SetHandler((InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var source = context.ParseResult.GetValueForArgument(sourceArg);
            var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);

            try
            {
                var installed = ForestStore.InstallPlan(forestDir, source);
                if (output.Json)
                {
                    output.WriteJson(new { status = "installed", source, planId = installed.Id, version = installed.Version });
                }
                else
                {
                    output.WriteLine($"Installed plan from: {source}");
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
            catch (ForestStore.PlanSourceNotFoundException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "plan_not_found", message: "Plan file not found", details: new { source });
                }
                else
                {
                    output.WriteErrorLine("Error: plan file not found");
                }

                context.ExitCode = ExitCodes.PlanNotFound;
            }
            catch (InvalidDataException ex)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "schema_validation_failed", message: "Invalid plan YAML", details: new { error = ex.Message });
                }
                else
                {
                    output.WriteErrorLine("Error: invalid plan YAML");
                }

                context.ExitCode = ExitCodes.SchemaValidationFailed;
            }
        });
        plansCommand.AddCommand(installCommand);

        return plansCommand;
    }
}


