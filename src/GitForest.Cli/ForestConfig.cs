using System.Text;

namespace GitForest.Cli;

public sealed record LlmConfig(
    string Provider,
    string Model,
    string BaseUrl,
    string ApiKeyEnvVar,
    double Temperature);

public sealed record ReconcileConfig(
    string Forum);

public sealed record ForestConfig(
    string PersistenceProvider,
    int LocksTimeoutSeconds,
    ReconcileConfig Reconcile,
    LlmConfig Llm);

public static class ForestConfigReader
{
    public const string DefaultPersistenceProvider = "file";
    public const int DefaultLocksTimeoutSeconds = 15;
    public const string DefaultReconcileForum = "file";
    public const string DefaultLlmProvider = "mock";
    public const string DefaultLlmModel = "gpt-4o-mini";
    public const string DefaultLlmBaseUrl = "https://api.openai.com/v1";
    public const string DefaultLlmApiKeyEnvVar = "OPENAI_API_KEY";
    public const double DefaultLlmTemperature = 0;

    private static readonly HashSet<string> AllowedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "file",
        "memory",
        "orleans"
    };

    private static readonly HashSet<string> AllowedLlmProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "mock",
        "openai",
        "ollama"
    };

    private static readonly HashSet<string> AllowedReconcileForums = new(StringComparer.OrdinalIgnoreCase)
    {
        "file",
        "ai"
    };

    public static ForestConfig ReadEffective(string forestDir)
    {
        var parsed = TryRead(forestDir);
        if (parsed is not null)
        {
            return parsed;
        }

        return new ForestConfig(
            PersistenceProvider: DefaultPersistenceProvider,
            LocksTimeoutSeconds: DefaultLocksTimeoutSeconds,
            Reconcile: new ReconcileConfig(Forum: DefaultReconcileForum),
            Llm: new LlmConfig(
                Provider: DefaultLlmProvider,
                Model: DefaultLlmModel,
                BaseUrl: DefaultLlmBaseUrl,
                ApiKeyEnvVar: DefaultLlmApiKeyEnvVar,
                Temperature: DefaultLlmTemperature));
    }

    public static ForestConfig? TryRead(string forestDir)
    {
        if (string.IsNullOrWhiteSpace(forestDir))
        {
            return null;
        }

        var configPath = Path.Combine(forestDir.Trim(), "config.yaml");
        if (!File.Exists(configPath))
        {
            return null;
        }

        string yaml;
        try
        {
            yaml = File.ReadAllText(configPath, Encoding.UTF8);
        }
        catch
        {
            // Best-effort: unreadable config falls back to defaults.
            return null;
        }

        return ParseEffective(yaml);
    }

    private static ForestConfig ParseEffective(string yaml)
    {
        var provider = DefaultPersistenceProvider;
        var locksTimeoutSeconds = DefaultLocksTimeoutSeconds;
        var reconcileForum = DefaultReconcileForum;
        var llmProvider = DefaultLlmProvider;
        var llmModel = DefaultLlmModel;
        var llmBaseUrl = DefaultLlmBaseUrl;
        var llmApiKeyEnvVar = DefaultLlmApiKeyEnvVar;
        var llmTemperature = DefaultLlmTemperature;

        var lines = SplitLines(yaml);
        string? currentSection = null;

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var trimmedStart = line.TrimStart();
            if (trimmedStart.StartsWith('#'))
            {
                continue;
            }

            if (!char.IsWhiteSpace(line[0]))
            {
                // Top-level key
                if (TryParseKeyValue(trimmedStart, out var key, out var value))
                {
                    currentSection = value.Length == 0 ? key : null;
                }
                else
                {
                    currentSection = null;
                }

                continue;
            }

            if (currentSection is null)
            {
                continue;
            }

            if (!TryParseKeyValue(trimmedStart, out var nestedKey, out var nestedValue))
            {
                continue;
            }

            if (currentSection.Equals("persistence", StringComparison.OrdinalIgnoreCase) &&
                nestedKey.Equals("provider", StringComparison.OrdinalIgnoreCase))
            {
                var candidate = nestedValue.Trim();
                if (AllowedProviders.Contains(candidate))
                {
                    provider = candidate.ToLowerInvariant();
                }

                continue;
            }

            if (currentSection.Equals("locks", StringComparison.OrdinalIgnoreCase) &&
                (nestedKey.Equals("timeoutSeconds", StringComparison.OrdinalIgnoreCase) ||
                 nestedKey.Equals("timeout_seconds", StringComparison.OrdinalIgnoreCase)))
            {
                if (int.TryParse(nestedValue.Trim(), out var seconds) && seconds > 0)
                {
                    locksTimeoutSeconds = seconds;
                }

                continue;
            }

            if (currentSection.Equals("reconcile", StringComparison.OrdinalIgnoreCase) &&
                nestedKey.Equals("forum", StringComparison.OrdinalIgnoreCase))
            {
                var candidate = nestedValue.Trim();
                if (AllowedReconcileForums.Contains(candidate))
                {
                    reconcileForum = candidate.ToLowerInvariant();
                }

                continue;
            }

            if (currentSection.Equals("llm", StringComparison.OrdinalIgnoreCase))
            {
                if (nestedKey.Equals("provider", StringComparison.OrdinalIgnoreCase))
                {
                    var candidate = nestedValue.Trim();
                    if (AllowedLlmProviders.Contains(candidate))
                    {
                        llmProvider = candidate.ToLowerInvariant();
                    }

                    continue;
                }

                if (nestedKey.Equals("model", StringComparison.OrdinalIgnoreCase))
                {
                    var candidate = nestedValue.Trim();
                    if (candidate.Length > 0)
                    {
                        llmModel = candidate;
                    }

                    continue;
                }

                if (nestedKey.Equals("baseUrl", StringComparison.OrdinalIgnoreCase) ||
                    nestedKey.Equals("base_url", StringComparison.OrdinalIgnoreCase))
                {
                    var candidate = nestedValue.Trim();
                    if (candidate.Length > 0)
                    {
                        llmBaseUrl = candidate;
                    }

                    continue;
                }

                if (nestedKey.Equals("apiKeyEnvVar", StringComparison.OrdinalIgnoreCase) ||
                    nestedKey.Equals("api_key_env_var", StringComparison.OrdinalIgnoreCase))
                {
                    var candidate = nestedValue.Trim();
                    if (candidate.Length > 0)
                    {
                        llmApiKeyEnvVar = candidate;
                    }

                    continue;
                }

                if (nestedKey.Equals("temperature", StringComparison.OrdinalIgnoreCase))
                {
                    if (double.TryParse(nestedValue.Trim(), out var t) && t >= 0)
                    {
                        llmTemperature = t;
                    }
                }
            }
        }

        return new ForestConfig(
            PersistenceProvider: provider,
            LocksTimeoutSeconds: locksTimeoutSeconds,
            Reconcile: new ReconcileConfig(Forum: reconcileForum),
            Llm: new LlmConfig(
                Provider: llmProvider,
                Model: llmModel,
                BaseUrl: llmBaseUrl,
                ApiKeyEnvVar: llmApiKeyEnvVar,
                Temperature: llmTemperature));
    }

    private static bool TryParseKeyValue(string line, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;

        var idx = line.IndexOf(':', StringComparison.Ordinal);
        if (idx < 0)
        {
            return false;
        }

        key = line[..idx].Trim();
        value = line[(idx + 1)..].Trim();

        if (key.Length == 0)
        {
            return false;
        }

        value = Unquote(value);
        return true;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }

    private static string[] SplitLines(string yaml)
    {
        return (yaml ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
    }
}

