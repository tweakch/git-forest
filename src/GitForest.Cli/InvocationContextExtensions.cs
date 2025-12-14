using System.CommandLine;
using System.CommandLine.Invocation;

namespace GitForest.Cli;

public static class InvocationContextExtensions
{
    public static bool GetJson(this InvocationContext context, CliOptions options)
    {
        return context.ParseResult.GetValueForOption(options.Json);
    }

    public static Output GetOutput(this InvocationContext context, CliOptions options)
    {
        return Output.From(context.Console, context.GetJson(options));
    }
}


