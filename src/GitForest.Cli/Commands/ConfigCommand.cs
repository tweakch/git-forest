using System.CommandLine;
using System.CommandLine.Invocation;
using GitForest.Cli.Features.Config;
using MediatR;

namespace GitForest.Cli.Commands;

public static class ConfigCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var configCommand = new Command("config", "Manage configuration");

        var showCommand = new Command("show", "Show configuration");
        var effectiveOption = new Option<bool>("--effective", "Show effective configuration");
        showCommand.AddOption(effectiveOption);

        showCommand.SetHandler(async (InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var effective = context.ParseResult.GetValueForOption(effectiveOption);
            var result = await mediator.Send(new ShowConfigQuery(Effective: effective));

            if (output.Json)
            {
                output.WriteJson(new { config = result.Config });
            }
            else
            {
                if (effective)
                {
                    output.WriteLine("Configuration (effective):");
                }
                else
                {
                    output.WriteLine("Configuration:");
                }

                var provider = string.IsNullOrWhiteSpace(result.Config.PersistenceProvider) ? "(unset)" : result.Config.PersistenceProvider;
                var locks = result.Config.LocksTimeoutSeconds <= 0 ? "(unset)" : result.Config.LocksTimeoutSeconds.ToString();
                output.WriteLine($"persistence.provider: {provider}");
                output.WriteLine($"locks.timeoutSeconds: {locks}");

                var llmProvider = string.IsNullOrWhiteSpace(result.Config.Llm.Provider) ? "(unset)" : result.Config.Llm.Provider;
                var llmModel = string.IsNullOrWhiteSpace(result.Config.Llm.Model) ? "(unset)" : result.Config.Llm.Model;
                var llmBaseUrl = string.IsNullOrWhiteSpace(result.Config.Llm.BaseUrl) ? "(unset)" : result.Config.Llm.BaseUrl;
                var llmApiKeyEnvVar = string.IsNullOrWhiteSpace(result.Config.Llm.ApiKeyEnvVar) ? "(unset)" : result.Config.Llm.ApiKeyEnvVar;
                output.WriteLine($"llm.provider: {llmProvider}");
                output.WriteLine($"llm.model: {llmModel}");
                output.WriteLine($"llm.baseUrl: {llmBaseUrl}");
                output.WriteLine($"llm.apiKeyEnvVar: {llmApiKeyEnvVar}");
                output.WriteLine($"llm.temperature: {result.Config.Llm.Temperature}");
            }

            context.ExitCode = ExitCodes.Success;
        });

        configCommand.AddCommand(showCommand);
        return configCommand;
    }
}


