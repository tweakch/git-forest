using System.CommandLine;
using GitForest.Application.Configuration;
using GitForest.Application.Features.Config;
using GitForest.Mediator;

namespace GitForest.Cli.Commands;

public static class ConfigCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var configCommand = CliCommandBuilder.Create("config", "Manage configuration");

        var showCommand = CliCommandBuilder.Create("show", "Show configuration");
        var effectiveOption = new Option<bool>("--effective")
        {
            Description = "Show effective configuration",
        };
        showCommand.AddOption(effectiveOption);

        showCommand.Action(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var effective = parseResult.GetValue(effectiveOption);
                var forestDir = ForestStore.GetDefaultForestDir();
                var result = await mediator.Send(
                    new ShowConfigQuery(ForestDir: forestDir, Effective: effective),
                    token
                );

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

                    var provider = string.IsNullOrWhiteSpace(result.Config.PersistenceProvider)
                        ? "(unset)"
                        : result.Config.PersistenceProvider;
                    var locks =
                        result.Config.LocksTimeoutSeconds <= 0
                            ? "(unset)"
                            : result.Config.LocksTimeoutSeconds.ToString();
                    output.WriteLine($"persistence.provider: {provider}");
                    output.WriteLine($"locks.timeoutSeconds: {locks}");

                    var clusterId = string.IsNullOrWhiteSpace(result.Config.Orleans.ClusterId)
                        ? "(unset)"
                        : result.Config.Orleans.ClusterId;
                    var serviceId = string.IsNullOrWhiteSpace(result.Config.Orleans.ServiceId)
                        ? "(unset)"
                        : result.Config.Orleans.ServiceId;
                    var gatewayHost = string.IsNullOrWhiteSpace(result.Config.Orleans.GatewayHost)
                        ? "(unset)"
                        : result.Config.Orleans.GatewayHost;
                    var gatewayPort =
                        result.Config.Orleans.GatewayPort <= 0
                            ? "(unset)"
                            : result.Config.Orleans.GatewayPort.ToString();
                    output.WriteLine($"orleans.clusterId: {clusterId}");
                    output.WriteLine($"orleans.serviceId: {serviceId}");
                    output.WriteLine($"orleans.gatewayHost: {gatewayHost}");
                    output.WriteLine($"orleans.gatewayPort: {gatewayPort}");

                    var llmProvider = string.IsNullOrWhiteSpace(result.Config.Llm.Provider)
                        ? "(unset)"
                        : result.Config.Llm.Provider;
                    var llmModel = string.IsNullOrWhiteSpace(result.Config.Llm.Model)
                        ? "(unset)"
                        : result.Config.Llm.Model;
                    var llmBaseUrl = string.IsNullOrWhiteSpace(result.Config.Llm.BaseUrl)
                        ? "(unset)"
                        : result.Config.Llm.BaseUrl;
                    var llmApiKeyEnvVar = string.IsNullOrWhiteSpace(result.Config.Llm.ApiKeyEnvVar)
                        ? "(unset)"
                        : result.Config.Llm.ApiKeyEnvVar;
                    output.WriteLine($"llm.provider: {llmProvider}");
                    output.WriteLine($"llm.model: {llmModel}");
                    output.WriteLine($"llm.baseUrl: {llmBaseUrl}");
                    output.WriteLine($"llm.apiKeyEnvVar: {llmApiKeyEnvVar}");
                    output.WriteLine($"llm.temperature: {result.Config.Llm.Temperature}");
                }

                return ExitCodes.Success;
            }
        );

        configCommand.AddSubcommand(showCommand);

        var setCommand = CliCommandBuilder.Create("set", "Set a configuration value");
        var keyArg = new Argument<string>("key")
        {
            Description =
                "Config key (persistence.provider | orleans.gatewayHost | orleans.gatewayPort | orleans.clusterId | orleans.serviceId)",
        };
        var valueArg = new Argument<string>("value") { Description = "Value to set" };
        setCommand.AddArgument(keyArg);
        setCommand.AddArgument(valueArg);

        setCommand.Action(
            async (parseResult, token) =>
            {
                _ = token;
                var output = parseResult.GetOutput(cliOptions);
                var key = (parseResult.GetRequiredValue(keyArg) ?? string.Empty).Trim();
                var value = (parseResult.GetRequiredValue(valueArg) ?? string.Empty).Trim();

                if (key.Length == 0)
                {
                    output.WriteErrorLine("Error: key is required");
                    return ExitCodes.InvalidArguments;
                }

                var forestDir = ForestStore.GetDefaultForestDir();
                ForestStore.EnsureInitialized(forestDir);

                var configPath = Path.Combine(forestDir, "config.yaml");
                var current = ForestConfigReader.ReadEffective(forestDir);
                ForestConfig updated = current;

                if (key.Equals("persistence.provider", StringComparison.OrdinalIgnoreCase))
                {
                    var provider = value.ToLowerInvariant();
                    if (provider is not "orleans" and not "file" and not "memory")
                    {
                        output.WriteErrorLine(
                            "Error: provider must be one of: orleans, file, memory"
                        );
                        return ExitCodes.InvalidArguments;
                    }

                    updated = current with { PersistenceProvider = provider };
                }
                else if (key.Equals("orleans.gatewayHost", StringComparison.OrdinalIgnoreCase))
                {
                    if (value.Length == 0)
                    {
                        output.WriteErrorLine("Error: orleans.gatewayHost must not be empty");
                        return ExitCodes.InvalidArguments;
                    }

                    updated = current with
                    {
                        Orleans = current.Orleans with { GatewayHost = value },
                    };
                }
                else if (key.Equals("orleans.gatewayPort", StringComparison.OrdinalIgnoreCase))
                {
                    if (!int.TryParse(value, out var port) || port <= 0 || port > 65535)
                    {
                        output.WriteErrorLine(
                            "Error: orleans.gatewayPort must be an integer 1..65535"
                        );
                        return ExitCodes.InvalidArguments;
                    }

                    updated = current with
                    {
                        Orleans = current.Orleans with { GatewayPort = port },
                    };
                }
                else if (key.Equals("orleans.clusterId", StringComparison.OrdinalIgnoreCase))
                {
                    if (value.Length == 0)
                    {
                        output.WriteErrorLine("Error: orleans.clusterId must not be empty");
                        return ExitCodes.InvalidArguments;
                    }

                    updated = current with { Orleans = current.Orleans with { ClusterId = value } };
                }
                else if (key.Equals("orleans.serviceId", StringComparison.OrdinalIgnoreCase))
                {
                    if (value.Length == 0)
                    {
                        output.WriteErrorLine("Error: orleans.serviceId must not be empty");
                        return ExitCodes.InvalidArguments;
                    }

                    updated = current with { Orleans = current.Orleans with { ServiceId = value } };
                }
                else
                {
                    output.WriteErrorLine($"Error: unknown key: {key}");
                    return ExitCodes.InvalidArguments;
                }

                ForestConfigWriter.WriteConfigYaml(configPath, updated);

                if (output.Json)
                {
                    output.WriteJson(
                        new
                        {
                            status = "updated",
                            key,
                            value,
                        }
                    );
                }
                else
                {
                    output.WriteLine($"updated {key} = {value}");
                }

                return ExitCodes.Success;
            }
        );

        configCommand.AddSubcommand(setCommand);
        return configCommand.Build();
    }
}
