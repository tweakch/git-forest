using System.CommandLine;
using System.CommandLine.Invocation;
using GitForest.Cli.Commands;

namespace GitForest.Cli;

public static class CliApp
{
    public static Task<int> InvokeAsync(string[] args)
    {
        var rootCommand = BuildRootCommand();
        return rootCommand.InvokeAsync(args);
    }

    public static RootCommand BuildRootCommand()
    {
        var options = new CliOptions();

        var rootCommand = new RootCommand("git-forest (gf) - CLI for managing repository forests");
        rootCommand.AddGlobalOption(options.Json);

        rootCommand.AddCommand(InitCommand.Build(options));
        rootCommand.AddCommand(StatusCommand.Build(options));
        rootCommand.AddCommand(ConfigCommand.Build(options));
        rootCommand.AddCommand(PlansCommand.Build(options));
        rootCommand.AddCommand(PlanCommand.Build(options));
        rootCommand.AddCommand(PlantsCommand.Build(options));
        rootCommand.AddCommand(PlantCommand.Build(options));
        rootCommand.AddCommand(PlantersCommand.Build(options));
        rootCommand.AddCommand(PlanterCommand.Build(options));
        rootCommand.AddCommand(PlannersCommand.Build(options));
        rootCommand.AddCommand(PlannerCommand.Build(options));

        return rootCommand;
    }
}


