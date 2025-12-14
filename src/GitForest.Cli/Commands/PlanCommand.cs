using System.CommandLine;
using System.CommandLine.Invocation;

namespace GitForest.Cli.Commands;

public static class PlanCommand
{
    public static Command Build(CliOptions cliOptions)
    {
        var planCommand = new Command("plan", "Manage a specific plan");
        var planIdArg = new Argument<string>("plan-id", "Plan identifier");
        planCommand.AddArgument(planIdArg);

        var reconcileCommand = new Command("reconcile", "Reconcile plan to desired state");
        var updateOption = new Option<bool>("--update", "Update plan before reconciling");
        var dryRunOption = new Option<bool>("--dry-run", "Show what would be done without applying");
        reconcileCommand.AddOption(updateOption);
        reconcileCommand.AddOption(dryRunOption);

        reconcileCommand.SetHandler((InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var planId = context.ParseResult.GetValueForArgument(planIdArg);
            var update = context.ParseResult.GetValueForOption(updateOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);

            _ = update; // TODO: implement

            if (output.Json)
            {
                output.WriteJson(new { planId, status = "reconciled", dryRun });
            }
            else
            {
                output.WriteLine($"Reconciling plan '{planId}'...");
                output.WriteLine("Planners: +0 ~0 -0");
                output.WriteLine("Planters: +0 ~0 -0");
                output.WriteLine("Plants:   +0 ~0 -0 (archived 0)");
                output.WriteLine(dryRun ? "done (dry-run)" : "done");
            }

            context.ExitCode = ExitCodes.Success;
        });

        planCommand.AddCommand(reconcileCommand);
        return planCommand;
    }
}


