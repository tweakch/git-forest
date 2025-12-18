using System.CommandLine;
using GitForest.Cli;
using MediatR;
using AppForest = GitForest.Application.Features.Forest;

namespace GitForest.Cli.Commands;

public static class InitCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var command = new Command("init", "Initialize forest in current git repo");

        var forceOption = new Option<bool>("--force") { Description = "Force re-initialization" };
        var dirOption = new Option<string>("--dir")
        {
            Description = "Directory for forest state",
            DefaultValueFactory = _ => ".git-forest",
        };

        command.Options.Add(forceOption);
        command.Options.Add(dirOption);

        command.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var force = parseResult.GetValue(forceOption);
                var dir = parseResult.GetValue(dirOption);

                var result = await mediator.Send(
                    new AppForest.InitForestCommand(DirOptionValue: dir, Force: force),
                    token
                );

                if (output.Json)
                {
                    output.WriteJson(
                        new
                        {
                            status = "initialized",
                            directory = result.DirectoryOptionValue,
                            path = result.ForestDirPath,
                        }
                    );
                }
                else
                {
                    output.WriteLine($"initialized ({result.DirectoryOptionValue})");
                }

                return ExitCodes.Success;
            }
        );

        return command;
    }
}
