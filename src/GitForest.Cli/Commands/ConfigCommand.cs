using System.CommandLine;
using System.CommandLine.Invocation;

namespace GitForest.Cli.Commands;

public static class ConfigCommand
{
    public static Command Build(CliOptions cliOptions)
    {
        var configCommand = new Command("config", "Manage configuration");

        var showCommand = new Command("show", "Show configuration");
        var effectiveOption = new Option<bool>("--effective", "Show effective configuration");
        showCommand.AddOption(effectiveOption);

        showCommand.SetHandler((InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var effective = context.ParseResult.GetValueForOption(effectiveOption);

            _ = effective; // TODO: implement

            if (output.Json)
            {
                output.WriteJson(new { config = new { } });
            }
            else
            {
                output.WriteLine("Configuration: (empty)");
            }

            context.ExitCode = ExitCodes.Success;
        });

        configCommand.AddCommand(showCommand);
        return configCommand;
    }
}


