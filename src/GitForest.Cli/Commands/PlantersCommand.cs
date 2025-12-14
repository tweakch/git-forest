using System.CommandLine;
using System.CommandLine.Invocation;

namespace GitForest.Cli.Commands;

public static class PlantersCommand
{
    public static Command Build(CliOptions cliOptions)
    {
        var plantersCommand = new Command("planters", "Manage planters");

        var listCommand = new Command("list", "List planters");
        var builtinOption = new Option<bool>("--builtin", "Show only built-in planters");
        var customOption = new Option<bool>("--custom", "Show only custom planters");
        listCommand.AddOption(builtinOption);
        listCommand.AddOption(customOption);

        listCommand.SetHandler((InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var builtin = context.ParseResult.GetValueForOption(builtinOption);
            var custom = context.ParseResult.GetValueForOption(customOption);

            _ = builtin; // TODO: implement
            _ = custom;  // TODO: implement

            if (output.Json)
            {
                output.WriteJson(new { planters = Array.Empty<object>() });
            }
            else
            {
                output.WriteLine("No planters configured");
            }

            context.ExitCode = ExitCodes.Success;
        });

        plantersCommand.AddCommand(listCommand);
        return plantersCommand;
    }
}


