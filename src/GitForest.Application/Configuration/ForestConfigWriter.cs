using System.Text;

namespace GitForest.Application.Configuration;

public static class ForestConfigWriter
{
    public static void WriteConfigYaml(string configPath, ForestConfig config)
    {
        if (string.IsNullOrWhiteSpace(configPath))
            throw new ArgumentException("Config path must be provided.", nameof(configPath));
        if (config is null)
            throw new ArgumentNullException(nameof(config));

        var dir = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(configPath.Trim(), Serialize(config), Encoding.UTF8);
    }

    public static string Serialize(ForestConfig config)
    {
        if (config is null)
            throw new ArgumentNullException(nameof(config));

        var sb = new StringBuilder();
        sb.AppendLine("# Repo-level git-forest config");

        var provider = string.IsNullOrWhiteSpace(config.PersistenceProvider)
            ? ForestConfigReader.DefaultPersistenceProvider
            : config.PersistenceProvider.Trim().ToLowerInvariant();

        sb.AppendLine("persistence:");
        sb.AppendLine($"  provider: {provider}");

        var locks =
            config.LocksTimeoutSeconds <= 0
                ? ForestConfigReader.DefaultLocksTimeoutSeconds
                : config.LocksTimeoutSeconds;
        sb.AppendLine("locks:");
        sb.AppendLine($"  timeoutSeconds: {locks}");

        var orleans = config.Orleans;
        var clusterId = string.IsNullOrWhiteSpace(orleans.ClusterId)
            ? ForestConfigReader.DefaultOrleansClusterId
            : orleans.ClusterId.Trim();
        var serviceId = string.IsNullOrWhiteSpace(orleans.ServiceId)
            ? ForestConfigReader.DefaultOrleansServiceId
            : orleans.ServiceId.Trim();
        var gatewayHost = string.IsNullOrWhiteSpace(orleans.GatewayHost)
            ? ForestConfigReader.DefaultOrleansGatewayHost
            : orleans.GatewayHost.Trim();
        var gatewayPort =
            orleans.GatewayPort <= 0
                ? ForestConfigReader.DefaultOrleansGatewayPort
                : orleans.GatewayPort;

        sb.AppendLine("orleans:");
        sb.AppendLine($"  clusterId: {clusterId}");
        sb.AppendLine($"  serviceId: {serviceId}");
        sb.AppendLine($"  gatewayHost: {gatewayHost}");
        sb.AppendLine($"  gatewayPort: {gatewayPort}");

        var reconcileForum = string.IsNullOrWhiteSpace(config.Reconcile?.Forum)
            ? ForestConfigReader.DefaultReconcileForum
            : config.Reconcile.Forum.Trim().ToLowerInvariant();
        sb.AppendLine("reconcile:");
        sb.AppendLine($"  forum: {reconcileForum}");

        var llm = config.Llm;
        var llmProvider = string.IsNullOrWhiteSpace(llm.Provider)
            ? ForestConfigReader.DefaultLlmProvider
            : llm.Provider.Trim().ToLowerInvariant();
        var llmModel = string.IsNullOrWhiteSpace(llm.Model)
            ? ForestConfigReader.DefaultLlmModel
            : llm.Model.Trim();
        var llmBaseUrl = string.IsNullOrWhiteSpace(llm.BaseUrl)
            ? ForestConfigReader.DefaultLlmBaseUrl
            : llm.BaseUrl.Trim();
        var llmApiKeyEnvVar = string.IsNullOrWhiteSpace(llm.ApiKeyEnvVar)
            ? ForestConfigReader.DefaultLlmApiKeyEnvVar
            : llm.ApiKeyEnvVar.Trim();
        var llmTemp =
            llm.Temperature < 0 ? ForestConfigReader.DefaultLlmTemperature : llm.Temperature;

        sb.AppendLine("llm:");
        sb.AppendLine($"  provider: {llmProvider}");
        sb.AppendLine($"  model: {llmModel}");
        sb.AppendLine($"  baseUrl: {llmBaseUrl}");
        sb.AppendLine($"  apiKeyEnvVar: {llmApiKeyEnvVar}");
        sb.AppendLine($"  temperature: {llmTemp}");

        return sb.ToString();
    }
}
