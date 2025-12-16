using System.CommandLine;
using System.CommandLine.Invocation;
using AppForest = GitForest.Application.Features.Forest;
using MediatR;

namespace GitForest.Cli.Commands;

public static class InitCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var command = new Command("init", "Initialize forest in current git repo");

        var forceOption = new Option<bool>("--force", "Force re-initialization");
        var dirOption = new Option<string>("--dir", () => ".git-forest", "Directory for forest state");

        command.AddOption(forceOption);
        command.AddOption(dirOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var force = context.ParseResult.GetValueForOption(forceOption);
            var dir = context.ParseResult.GetValueForOption(dirOption);

            var result = await mediator.Send(new AppForest.InitForestCommand(DirOptionValue: dir, Force: force));

            if (output.Json)
            {
                output.WriteJson(new { status = "initialized", directory = result.DirectoryOptionValue, path = result.ForestDirPath });
            }
            else
            {
                output.WriteLine($"initialized ({result.DirectoryOptionValue})");
            }

            context.ExitCode = ExitCodes.Success;
        });

        return command;
    }
}


