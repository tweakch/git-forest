using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;

namespace GitForest.Cli.Commands;

public sealed class CliCommandBuilder
{
    private readonly Command command;

    private CliCommandBuilder(Command command)
    {
        this.command = command;
    }

    public static CliCommandBuilder Create(string name, string description)
    {
        return new CliCommandBuilder(new Command(name, description));
    }

    public CliCommandBuilder AddArgument<T>(Argument<T> argument)
    {
        command.Arguments.Add(argument);
        return this;
    }

    public CliCommandBuilder AddOption<T>(Option<T> option)
    {
        command.Options.Add(option);
        return this;
    }

    public CliCommandBuilder AddSubcommand(Command subcommand)
    {
        command.Subcommands.Add(subcommand);
        return this;
    }

    public CliCommandBuilder AddSubcommand(CliCommandBuilder subcommand)
    {
        return AddSubcommand(subcommand.Build());
    }

    public CliCommandBuilder Subcommand(
        string name,
        string description,
        System.Action<CliCommandBuilder> configure
    )
    {
        var builder = Create(name, description);
        configure(builder);
        return AddSubcommand(builder);
    }

    public CliCommandBuilder Action(Func<ParseResult, CancellationToken, Task<int>> handler)
    {
        command.SetAction(handler);
        return this;
    }

    public Command Build()
    {
        return command;
    }

    public CliCommandBuilder AddArgument<T>(string name, string description)
    {
        return AddArgument(new Argument<T>(name) { Description = description });
    }
}
