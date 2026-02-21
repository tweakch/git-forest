using System.CommandLine;
using GitForest.Cli.Commands;
using NUnit.Framework;

namespace GitForest.Cli.Tests;

public class CliCommandBuilderTests
{
    [Test]
    public async Task Builder_WiresOptionArgumentSubcommand_And_Action()
    {
        var handlerInvoked = false;

        var demoCommand = CliCommandBuilder
            .Create("demo", "Demo command")
            .Subcommand(
                "echo",
                "Echo command",
                echoCommand =>
                {
                    var nameOption = new Option<string>("--name") { Description = "Name" };
                    var countArg = new Argument<int>("count") { Description = "Count" };

                    echoCommand
                        .AddOption(nameOption)
                        .AddArgument(countArg)
                        .Action(
                            (parseResult, token) =>
                            {
                                _ = token;
                                handlerInvoked = true;

                                var name = parseResult.GetValue(nameOption);
                                var count = parseResult.GetValue(countArg);
                                return Task.FromResult(name == "ok" ? count : -1);
                            }
                        );
                }
            )
            .Build();

        var root = new RootCommand();
        root.Subcommands.Add(demoCommand);

        var parseResult = root.Parse(new[] { "demo", "echo", "--name", "ok", "5" });
        var exitCode = await parseResult.InvokeAsync();

        Assert.That(exitCode, Is.EqualTo(5));
        Assert.That(handlerInvoked, Is.True);
    }
}
