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
            if (output.Json)
            {
                output.WriteJson(new { plans = Array.Empty<object>() });
            }
            else
            {
                output.WriteLine("No plans installed");
            }

            context.ExitCode = ExitCodes.Success;
        });
        plansCommand.AddCommand(listCommand);

        var installCommand = new Command("install", "Install a plan");
        var sourceArg = new Argument<string>("source", "Plan source (GitHub slug, URL, or local path)");
        installCommand.AddArgument(sourceArg);
        installCommand.SetHandler((InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var source = context.ParseResult.GetValueForArgument(sourceArg);
            if (output.Json)
            {
                output.WriteJson(new { status = "installed", source });
            }
            else
            {
                output.WriteLine($"Installed plan from: {source}");
            }

            context.ExitCode = ExitCodes.Success;
        });
        plansCommand.AddCommand(installCommand);

        return plansCommand;
    }
}


