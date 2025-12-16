using System.Text;

namespace GitForest.Cli;

public sealed record ForestConfig(
    string PersistenceProvider,
    int LocksTimeoutSeconds);

public static class ForestConfigReader
{
    public const string DefaultPersistenceProvider = "file";
    public const int DefaultLocksTimeoutSeconds = 15;

    private static readonly HashSet<string> AllowedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "file",
        "memory",
        "orleans"
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
            LocksTimeoutSeconds: DefaultLocksTimeoutSeconds);
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
            }
        }

        return new ForestConfig(
            PersistenceProvider: provider,
            LocksTimeoutSeconds: locksTimeoutSeconds);
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

