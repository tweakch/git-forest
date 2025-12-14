using System.CommandLine;
using System.CommandLine.Invocation;

namespace GitForest.Cli.Commands;

public static class StatusCommand
{
    public static Command Build(CliOptions cliOptions)
    {
        var command = new Command("status", "Show forest status");

        command.SetHandler((InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);

            if (output.Json)
            {
                output.WriteJson(new
                {
                    forest = "initialized",
                    repo = "origin/main",
                    plans = 0,
                    plants = 0,
                    planters = 0,
                    @lock = "free"
                });
            }
            else
            {
                output.WriteLine("Forest: initialized  Repo: origin/main");
                output.WriteLine("Plans: 0 installed");
                output.WriteLine("Plants: planned 0 | planted 0 | growing 0 | harvestable 0 | harvested 0");
                output.WriteLine("Planters: 0 available | 0 active");
                output.WriteLine("Lock: free");
            }

            context.ExitCode = ExitCodes.Success;
        });

        return command;
    }
}


