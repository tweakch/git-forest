using System.CommandLine;

namespace GitForest.Cli;

public static class ParseResultExtensions
{
    public static bool GetJson(this ParseResult parseResult, CliOptions options)
    {
        return parseResult.GetValue(options.Json);
    }

    public static Output GetOutput(this ParseResult parseResult, CliOptions options)
    {
        return Output.From(parseResult.GetJson(options));
    }
}
