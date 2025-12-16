using System.Globalization;
using System.Text;
using GitForest.Core.Services;

namespace GitForest.Infrastructure.FileSystem.Forest;

public sealed class FileSystemForestInitializer : IForestInitializer
{
    public void Initialize(string forestDir)
    {
        if (string.IsNullOrWhiteSpace(forestDir))
        {
            throw new ArgumentException("Forest directory must be provided.", nameof(forestDir));
        }

        var dir = forestDir.Trim();
        Directory.CreateDirectory(dir);

        // Required folders (CLI.md §12)
        Directory.CreateDirectory(Path.Combine(dir, "plans"));
        Directory.CreateDirectory(Path.Combine(dir, "plants"));
        Directory.CreateDirectory(Path.Combine(dir, "planters"));
        Directory.CreateDirectory(Path.Combine(dir, "planners"));
        Directory.CreateDirectory(Path.Combine(dir, "logs"));

        // Required files
        var forestYamlPath = Path.Combine(dir, "forest.yaml");
        if (!File.Exists(forestYamlPath))
        {
            var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            File.WriteAllText(
                forestYamlPath,
                $"version: v0{Environment.NewLine}initialized_at: {now}{Environment.NewLine}",
                Encoding.UTF8);
        }

        var configYamlPath = Path.Combine(dir, "config.yaml");
        if (!File.Exists(configYamlPath))
        {
            // Keep defaults aligned with ForestConfigReader:
            // persistence.provider=file, locks.timeoutSeconds=15, llm.provider=mock
            File.WriteAllText(
                configYamlPath,
                $"# Repo-level git-forest config{Environment.NewLine}" +
                $"persistence:{Environment.NewLine}  provider: file{Environment.NewLine}" +
                $"locks:{Environment.NewLine}  timeoutSeconds: 15{Environment.NewLine}" +
                $"llm:{Environment.NewLine}" +
                $"  provider: mock{Environment.NewLine}" +
                $"  model: gpt-4o-mini{Environment.NewLine}" +
                $"  baseUrl: https://api.openai.com/v1{Environment.NewLine}" +
                $"  apiKeyEnvVar: OPENAI_API_KEY{Environment.NewLine}" +
                $"  temperature: 0{Environment.NewLine}",
                Encoding.UTF8);
        }

        var lockPath = Path.Combine(dir, "lock");
        if (!File.Exists(lockPath))
        {
            File.WriteAllText(lockPath, string.Empty, Encoding.UTF8);
        }
    }
}

