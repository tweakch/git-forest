using System.CommandLine;
using System.CommandLine.Invocation;

namespace GitForest.Cli.Commands;

public static class PlannersCommand
{
    public static Command Build(CliOptions cliOptions)
    {
        var plannersCommand = new Command("planners", "Manage planners");

        var listCommand = new Command("list", "List planners");
        var planFilterOption = new Option<string?>("--plan", "Filter by plan ID");
        listCommand.AddOption(planFilterOption);

        listCommand.SetHandler((InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var plan = context.ParseResult.GetValueForOption(planFilterOption);

            _ = plan; // TODO: implement

            if (output.Json)
            {
                output.WriteJson(new { planners = Array.Empty<object>() });
            }
            else
            {
                output.WriteLine("No planners configured");
            }

            context.ExitCode = ExitCodes.Success;
        });

        plannersCommand.AddCommand(listCommand);
        return plannersCommand;
    }
}


