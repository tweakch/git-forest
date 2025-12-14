using System.CommandLine;
using System.CommandLine.Invocation;

namespace GitForest.Cli.Commands;

public static class InitCommand
{
    public static Command Build(CliOptions cliOptions)
    {
        var command = new Command("init", "Initialize forest in current git repo");

        var forceOption = new Option<bool>("--force", "Force re-initialization");
        var dirOption = new Option<string>("--dir", () => ".git-forest", "Directory for forest state");

        command.AddOption(forceOption);
        command.AddOption(dirOption);

        command.SetHandler((InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var force = context.ParseResult.GetValueForOption(forceOption);
            var dir = context.ParseResult.GetValueForOption(dirOption);

            _ = force; // TODO: implement

            if (output.Json)
            {
                output.WriteJson(new { status = "initialized", directory = dir });
            }
            else
            {
                output.WriteLine($"initialized ({dir})");
            }

            context.ExitCode = ExitCodes.Success;
        });

        return command;
    }
}


