using System.CommandLine;
using System.CommandLine.Invocation;

namespace GitForest.Cli.Commands;

public static class PlannerCommand
{
    public static Command Build(CliOptions cliOptions)
    {
        var plannerCommand = new Command("planner", "Manage a specific planner");
        var plannerIdArg = new Argument<string>("planner-id", "Planner identifier");
        plannerCommand.AddArgument(plannerIdArg);

        var runCommand = new Command("run", "Run planner");
        var planOption = new Option<string>("--plan", "Plan ID to run against") { IsRequired = true };
        runCommand.AddOption(planOption);

        runCommand.SetHandler((InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var plannerId = context.ParseResult.GetValueForArgument(plannerIdArg);
            var plan = context.ParseResult.GetValueForOption(planOption);

            if (output.Json)
            {
                output.WriteJson(new { plannerId, plan, status = "completed" });
            }
            else
            {
                output.WriteLine($"Running planner '{plannerId}' for plan '{plan}'...");
                output.WriteLine("done");
            }

            context.ExitCode = ExitCodes.Success;
        });

        plannerCommand.AddCommand(runCommand);
        return plannerCommand;
    }
}


