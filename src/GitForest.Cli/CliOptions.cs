using System.CommandLine;

namespace GitForest.Cli;

public sealed class CliOptions
{
    public Option<bool> Json { get; } = new("--json", "Output in JSON format");
}


