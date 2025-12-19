using System;
using System.IO;
using System.Text;
using GitForest.Infrastructure.FileSystem.Serialization;
using NUnit.Framework;

namespace GitForest.Cli.Tests;

[TestFixture]
[NonParallelizable]
public sealed class EvolveCliTests
{
    [Test]
    public async Task Evolve_UpdatesAllPlants_WithBranchAndPlanter()
    {
        using var env = new CliTestEnv();
        env.EnsureForestInitialized();
        env.WritePlant(key: "plan-a:alpha", status: "planned", title: "Alpha");
        env.WritePlant(key: "plan-b:beta", status: "planted", title: "Beta");

        using var console = new ConsoleCapture();
        var exitCode = await CliApp.InvokeAsync("evolve", "--planter", "p1");

        Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));

        var plantA = env.ReadPlant("plan-a:alpha");
        Assert.That(plantA.Status, Is.EqualTo("growing"));
        Assert.That(plantA.AssignedPlanters, Does.Contain("p1"));
        Assert.That(plantA.Branches, Does.Contain("p1/plan-a__alpha"));

        var plantB = env.ReadPlant("plan-b:beta");
        Assert.That(plantB.Status, Is.EqualTo("growing"));
        Assert.That(plantB.AssignedPlanters, Does.Contain("p1"));
        Assert.That(plantB.Branches, Does.Contain("p1/plan-b__beta"));
    }

    private sealed class CliTestEnv : IDisposable
    {
        private readonly string _originalCwd;
        private readonly string _workDir;
        private readonly string _forestDir;

        public CliTestEnv()
        {
            _originalCwd = Environment.CurrentDirectory;
            _workDir = Path.Combine(
                Path.GetTempPath(),
                "git-forest",
                "tests",
                Guid.NewGuid().ToString("n")
            );
            Directory.CreateDirectory(_workDir);
            Environment.CurrentDirectory = _workDir;

            _forestDir = Path.Combine(_workDir, ".git-forest");
        }

        public void EnsureForestInitialized()
        {
            Directory.CreateDirectory(_forestDir);

            // ForestStore.IsInitialized() requires forest.yaml.
            var forestYamlPath = Path.Combine(_forestDir, "forest.yaml");
            if (!File.Exists(forestYamlPath))
            {
                File.WriteAllText(forestYamlPath, "version: v0\n", Encoding.UTF8);
            }

            // Create config.yaml to use file persistence for tests (not Orleans).
            var configYamlPath = Path.Combine(_forestDir, "config.yaml");
            if (!File.Exists(configYamlPath))
            {
                File.WriteAllText(
                    configYamlPath,
                    "persistence:\n  provider: file\n",
                    Encoding.UTF8
                );
            }
        }

        public void WritePlant(string key, string status, string title)
        {
            var parts = (key ?? string.Empty).Split(':', 2, StringSplitOptions.TrimEntries);
            if (
                parts.Length != 2
                || string.IsNullOrWhiteSpace(parts[0])
                || string.IsNullOrWhiteSpace(parts[1])
            )
            {
                throw new ArgumentException(
                    $"Invalid plant key '{key}'. Expected: <plan-id>:<slug>.",
                    nameof(key)
                );
            }

            var planId = parts[0];
            var slug = parts[1];
            var safeKey = $"{planId}:{slug}";

            var plantDir = Path.Combine(_forestDir, "plants", $"{planId}__{slug}");
            Directory.CreateDirectory(plantDir);

            var plant = new PlantFileModel(
                Key: safeKey,
                Status: status,
                Title: title,
                PlanId: planId,
                PlannerId: null,
                AssignedPlanters: Array.Empty<string>(),
                Branches: Array.Empty<string>(),
                SelectedBranch: null,
                CreatedAt: "2020-01-01T00:00:00Z",
                UpdatedAt: null,
                Description: null
            );

            var yaml = PlantYamlLite.Serialize(plant);
            File.WriteAllText(Path.Combine(plantDir, "plant.yaml"), yaml, Encoding.UTF8);
        }

        public PlantFileModel ReadPlant(string key)
        {
            var parts = (key ?? string.Empty).Split(':', 2, StringSplitOptions.TrimEntries);
            if (
                parts.Length != 2
                || string.IsNullOrWhiteSpace(parts[0])
                || string.IsNullOrWhiteSpace(parts[1])
            )
            {
                throw new ArgumentException(
                    $"Invalid plant key '{key}'. Expected: <plan-id>:<slug>.",
                    nameof(key)
                );
            }

            var planId = parts[0];
            var slug = parts[1];

            var plantDir = Path.Combine(_forestDir, "plants", $"{planId}__{slug}");
            var path = Path.Combine(plantDir, "plant.yaml");
            var yaml = File.ReadAllText(path, Encoding.UTF8);
            return PlantYamlLite.Parse(yaml);
        }

        public void Dispose()
        {
            Environment.CurrentDirectory = _originalCwd;

            try
            {
                if (Directory.Exists(_workDir))
                {
                    Directory.Delete(_workDir, recursive: true);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    private sealed class ConsoleCapture : IDisposable
    {
        private readonly TextWriter _originalOut;
        private readonly TextWriter _originalError;
        private readonly StringWriter _out;
        private readonly StringWriter _err;

        public ConsoleCapture()
        {
            _originalOut = Console.Out;
            _originalError = Console.Error;

            _out = new StringWriter();
            _err = new StringWriter();

            Console.SetOut(_out);
            Console.SetError(_err);
        }

        public string StdOut => _out.ToString();
        public string StdErr => _err.ToString();

        public void Dispose()
        {
            Console.SetOut(_originalOut);
            Console.SetError(_originalError);

            _out.Dispose();
            _err.Dispose();
        }
    }
}
