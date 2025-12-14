using System.CommandLine;
using System.CommandLine.Invocation;

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

            // TODO: Implement actual planter lookup
            if (output.Json)
            {
                output.WriteJsonError(code: "planter_not_found", message: "Planter not found", details: new { planterId });
            }
            else
            {
                output.WriteErrorLine($"Planter '{planterId}': not found");
            }

            context.ExitCode = ExitCodes.PlanterNotFound;
        });

        planterCommand.AddCommand(showCommand);
        return planterCommand;
    }
}


