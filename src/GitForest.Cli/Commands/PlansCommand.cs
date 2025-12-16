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
                        plans = plans.Select(p => new
                        {
                            id = p.Id,
                            name = p.Name,
                            version = p.Version,
                            category = p.Category,
                            author = p.Author,
                            license = p.License,
                            repository = p.Repository,
                            homepage = p.Homepage,
                            source = p.Source,
                            installedAt = p.InstalledAt,
                            sha256 = p.Sha256
                        }).ToArray()
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
                        output.WriteLine(
                            $"{PadRight("Id", 28)} {PadRight("Version", 10)} {PadRight("Category", 20)} {PadRight("Author", 18)} {PadRight("License", 12)} {PadRight("Repository", 26)} {PadRight("InstalledAt", 20)} {PadRight("SHA256", 10)} {PadRight("Source", 20)}");

                        foreach (var plan in plans)
                        {
                            var version = string.IsNullOrWhiteSpace(plan.Version) ? "-" : plan.Version;
                            var category = string.IsNullOrWhiteSpace(plan.Category) ? "-" : plan.Category;
                            var author = string.IsNullOrWhiteSpace(plan.Author) ? "-" : plan.Author;
                            var license = string.IsNullOrWhiteSpace(plan.License) ? "-" : plan.License;
                            var repository = string.IsNullOrWhiteSpace(plan.Repository) ? "-" : plan.Repository;
                            var installedAt = FormatInstalledAt(plan.InstalledAt);
                            var sha = ShortSha(plan.Sha256);
                            var source = string.IsNullOrWhiteSpace(plan.Source) ? "-" : plan.Source;

                            output.WriteLine(
                                $"{PadRight(plan.Id, 28)} {PadRight(version, 10)} {PadRight(Truncate(category, 20), 20)} {PadRight(Truncate(author, 18), 18)} {PadRight(Truncate(license, 12), 12)} {PadRight(Truncate(repository, 26), 26)} {PadRight(installedAt, 20)} {PadRight(sha, 10)} {PadRight(Truncate(source, 20), 20)}");
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

    private static string PadRight(string value, int width)
    {
        value ??= string.Empty;
        return value.Length >= width ? value : value.PadRight(width);
    }

    private static string Truncate(string value, int max)
    {
        value ??= string.Empty;
        return value.Length <= max ? value : value[..Math.Max(0, max - 3)] + "...";
    }

    private static string ShortSha(string value)
    {
        value ??= string.Empty;
        if (value.Length == 0)
        {
            return "-";
        }

        return value.Length <= 10 ? value : value[..10];
    }

    private static string FormatInstalledAt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        // Common ISO-8601 timestamps are long; trim to fit the table.
        return value.Length <= 20 ? value : value[..20];
    }
}


