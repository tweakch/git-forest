using System.CommandLine;

namespace GitForest.Cli.Commands;

public static class EvolveCommand
{
    public static Command Build(CliOptions cliOptions)
    {
        var command = new Command("evolve", "Evolve forest (scaffold)");

        command.SetAction(
            (parseResult, token) =>
            {
                _ = token;
                var output = parseResult.GetOutput(cliOptions);
                const string message = "Evolve workflow not implemented yet.";

                if (output.Json)
                {
                    output.WriteJson(new { status = "not_implemented", message });
                }
                else
                {
                    output.WriteLine(message);
                }

                return Task.FromResult(ExitCodes.Success);
            }
        );

        return command;
    }
}
