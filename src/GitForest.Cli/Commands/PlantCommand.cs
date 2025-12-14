using System.CommandLine;
using System.CommandLine.Invocation;

namespace GitForest.Cli.Commands;

public static class PlantCommand
{
    public static Command Build(CliOptions cliOptions)
    {
        var plantCommand = new Command("plant", "Manage a specific plant");
        var selectorArg = new Argument<string>("selector", "Plant selector (key, slug, or P01)");
        plantCommand.AddArgument(selectorArg);

        var showCommand = new Command("show", "Show plant details");
        showCommand.SetHandler((InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var selector = context.ParseResult.GetValueForArgument(selectorArg);

            // TODO: Implement actual plant lookup
            if (output.Json)
            {
                output.WriteJsonError(code: "plant_not_found", message: "Plant not found", details: new { selector });
            }
            else
            {
                output.WriteErrorLine($"Plant '{selector}': not found");
            }

            context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
        });

        plantCommand.AddCommand(showCommand);
        return plantCommand;
    }
}


