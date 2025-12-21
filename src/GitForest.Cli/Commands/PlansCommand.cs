using System.CommandLine;
using GitForest.Mediator;
using AppPlans = GitForest.Application.Features.Plans;

namespace GitForest.Cli.Commands;

public static class PlansCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        return CliCommandBuilder
            .Create("plans", "Manage plans")
            .Subcommand("list", "List installed plans", ConfigureListCommand)
            .Subcommand("install", "Install a plan", ConfigureInstallCommand)
            .Build();

        void ConfigureListCommand(CliCommandBuilder listCommand)
        {
            listCommand.Action(
                async (parseResult, token) =>
                {
                    var output = parseResult.GetOutput(cliOptions);

                    try
                    {
                        var forestDir = ForestStore.GetDefaultForestDir();
                        ForestStore.EnsureInitialized(forestDir);

                        var plans = await mediator.Send(new AppPlans.ListPlansQuery(), token);
                        if (output.Json)
                        {
                            output.WriteJson(
                                new
                                {
                                    plans = plans
                                        .Select(p => new
                                        {
                                            id = p.Id,
                                            name = p.Id,
                                            version = p.Version,
                                            category = string.Empty,
                                            author = p.Author,
                                            license = p.License,
                                            repository = p.Repository,
                                            homepage = p.Homepage,
                                            source = p.Source,
                                            installedAt = p.InstalledDate == default
                                                ? string.Empty
                                                : p.InstalledDate.ToString("O"),
                                            sha256 = string.Empty,
                                        })
                                        .ToArray(),
                                }
                            );
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
                                    $"{PadRight("Id", 28)} {PadRight("Version", 10)} {PadRight("Category", 20)} {PadRight("InstalledAt", 20)}"
                                );

                                foreach (var plan in plans)
                                {
                                    var version = string.IsNullOrWhiteSpace(plan.Version)
                                        ? "-"
                                        : plan.Version;
                                    var category = "-";
                                    var installedAt =
                                        plan.InstalledDate == default
                                            ? "-"
                                            : FormatInstalledAt(plan.InstalledDate.ToString("O"));

                                    output.WriteLine(
                                        $"{PadRight(plan.Id, 28)} {PadRight(version, 10)} {PadRight(Truncate(category, 20), 20)} {PadRight(installedAt, 20)}"
                                    );
                                }
                            }
                        }

                        return ExitCodes.Success;
                    }
                    catch (ForestStore.ForestNotInitializedException)
                    {
                        return BaseCommand.WriteForestNotInitialized(output);
                    }
                }
            );
        }

        void ConfigureInstallCommand(CliCommandBuilder installCommand)
        {
            var sourceArg = new Argument<string>("source")
            {
                Description = "Plan source (GitHub slug, URL, or local path)",
            };

            installCommand
                .AddArgument(sourceArg)
                .Action(
                    async (parseResult, token) =>
                    {
                        var output = parseResult.GetOutput(cliOptions);
                        var source = parseResult.GetRequiredValue(sourceArg);

                        try
                        {
                            var forestDir = ForestStore.GetDefaultForestDir();
                            ForestStore.EnsureInitialized(forestDir);

                            var installed = await mediator.Send(
                                new AppPlans.InstallPlanCommand(Source: source),
                                token
                            );
                            if (output.Json)
                            {
                                output.WriteJson(
                                    new
                                    {
                                        status = "installed",
                                        source,
                                        planId = installed.Id,
                                        version = installed.Version,
                                    }
                                );
                            }
                            else
                            {
                                output.WriteLine($"Installed plan from: {source}");
                            }

                            return ExitCodes.Success;
                        }
                        catch (ForestStore.ForestNotInitializedException)
                        {
                            return BaseCommand.WriteForestNotInitialized(output);
                        }
                        catch (FileNotFoundException)
                        {
                            if (output.Json)
                            {
                                output.WriteJsonError(
                                    code: "plan_not_found",
                                    message: "Plan file not found",
                                    details: new { source }
                                );
                            }
                            else
                            {
                                output.WriteErrorLine("Error: plan file not found");
                            }

                            return ExitCodes.PlanNotFound;
                        }
                        catch (InvalidDataException ex)
                        {
                            if (output.Json)
                            {
                                output.WriteJsonError(
                                    code: "schema_validation_failed",
                                    message: "Invalid plan YAML",
                                    details: new { error = ex.Message }
                                );
                            }
                            else
                            {
                                output.WriteErrorLine("Error: invalid plan YAML");
                            }

                            return ExitCodes.SchemaValidationFailed;
                        }
                    }
                );
        }
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
