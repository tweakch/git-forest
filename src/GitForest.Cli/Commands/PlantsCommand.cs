using System.CommandLine;
using System.CommandLine.Invocation;

namespace GitForest.Cli.Commands;

public static class PlantsCommand
{
    public static Command Build(CliOptions cliOptions)
    {
        var plantsCommand = new Command("plants", "Manage plants");

        var listCommand = new Command("list", "List plants");
        var statusFilterOption = new Option<string?>("--status", "Filter by status (planned|planted|growing|harvestable|harvested|archived)");
        var planFilterOption = new Option<string?>("--plan", "Filter by plan ID");
        listCommand.AddOption(statusFilterOption);
        listCommand.AddOption(planFilterOption);

        listCommand.SetHandler((InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var status = context.ParseResult.GetValueForOption(statusFilterOption);
            var plan = context.ParseResult.GetValueForOption(planFilterOption);

            _ = status; // TODO: implement filtering
            _ = plan;   // TODO: implement filtering

            if (output.Json)
            {
                output.WriteJson(new { plants = Array.Empty<object>() });
            }
            else
            {
                output.WriteLine("Key                             Status   Title                         Plan   Planter");
                output.WriteLine("No plants found");
            }

            context.ExitCode = ExitCodes.Success;
        });

        plantsCommand.AddCommand(listCommand);
        return plantsCommand;
    }
}


