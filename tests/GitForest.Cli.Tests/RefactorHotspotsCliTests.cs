using System.Text;
using System.Text.Json;
using GitForest.Infrastructure.FileSystem.Serialization;
using NUnit.Framework;

namespace GitForest.Cli.Tests;

[TestFixture]
[NonParallelizable]
public sealed class RefactorHotspotsCliTests
{
    [Test]
    public async Task PlantShow_ByExactKey_PrintsPlantKey_WhenForestInitialized()
    {
        using var env = new CliTestEnv();
        env.EnsureForestInitialized();
        env.WritePlant(key: "plan-a:alpha", status: "planned", title: "Alpha");

        using var console = new ConsoleCapture();
        var exitCode = await CliApp.InvokeAsync(
            new[] { "plant", "plan-a:alpha", "show", "--json" }
        );

        Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));

        var doc = JsonDocument.Parse(console.StdOut);
        Assert.That(doc.RootElement.TryGetProperty("plant", out var plant), Is.True);
        Assert.That(plant.GetProperty("key").GetString(), Is.EqualTo("plan-a:alpha"));
        Assert.That(plant.GetProperty("title").GetString(), Is.EqualTo("Alpha"));
    }

    [Test]
    public async Task PlantShow_ByPIndex_ResolvesDeterministically_ByOrderedKey()
    {
        using var env = new CliTestEnv();
        env.EnsureForestInitialized();

        // Keys order (case-insensitive): plan-a:alpha, plan-a:beta, plan-b:alpha
        env.WritePlant(key: "plan-a:alpha", status: "planned", title: "A");
        env.WritePlant(key: "plan-a:beta", status: "planned", title: "B");
        env.WritePlant(key: "plan-b:alpha", status: "planned", title: "C");

        using var console = new ConsoleCapture();
        var exitCode = await CliApp.InvokeAsync(new[] { "plant", "P02", "show", "--json" });

        Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));

        var doc = JsonDocument.Parse(console.StdOut);
        Assert.That(
            doc.RootElement.GetProperty("plant").GetProperty("key").GetString(),
            Is.EqualTo("plan-a:beta")
        );
    }

    [Test]
    public async Task PlantShow_BySlug_WhenAmbiguous_ReturnsPlantAmbiguous_AndIncludesMatches()
    {
        using var env = new CliTestEnv();
        env.EnsureForestInitialized();

        env.WritePlant(key: "plan-a:alpha", status: "planned", title: "A");
        env.WritePlant(key: "plan-b:alpha", status: "planned", title: "B");

        using var console = new ConsoleCapture();
        var exitCode = await CliApp.InvokeAsync(new[] { "plant", "alpha", "show", "--json" });

        Assert.That(exitCode, Is.EqualTo(ExitCodes.PlantNotFoundOrAmbiguous));

        var doc = JsonDocument.Parse(console.StdOut);
        var err = doc.RootElement.GetProperty("error");
        Assert.That(err.GetProperty("code").GetString(), Is.EqualTo("plant_ambiguous"));

        var details = err.GetProperty("details");
        Assert.That(details.GetProperty("selector").GetString(), Is.EqualTo("alpha"));

        var matches = details.GetProperty("matches");
        Assert.That(matches.ValueKind, Is.EqualTo(JsonValueKind.Array));
        var matchStrings = new[] { matches[0].GetString(), matches[1].GetString() };
        Assert.That(matchStrings, Is.EquivalentTo(new[] { "plan-a:alpha", "plan-b:alpha" }));
    }

    [Test]
    public async Task PlanterPlant_WithoutYes_ReturnsConfirmationRequired_WithComputedBranch()
    {
        using var env = new CliTestEnv();
        env.EnsureForestInitialized();

        env.WriteCustomPlanter(planterId: "p1");
        env.WritePlant(key: "plan-a:alpha", status: "planned", title: "Alpha");

        using var console = new ConsoleCapture();
        var exitCode = await CliApp.InvokeAsync(
            new[] { "planter", "p1", "plant", "plan-a:alpha", "--json" }
        );

        Assert.That(exitCode, Is.EqualTo(ExitCodes.InvalidArguments));

        var doc = JsonDocument.Parse(console.StdOut);
        var err = doc.RootElement.GetProperty("error");
        Assert.That(err.GetProperty("code").GetString(), Is.EqualTo("confirmation_required"));

        var details = err.GetProperty("details");
        Assert.That(details.GetProperty("branch").GetString(), Is.EqualTo("p1/plan-a__alpha"));
    }

    [Test]
    public async Task PlanterGrow_WithInvalidMode_ReturnsInvalidArguments_WithModeEchoed()
    {
        using var env = new CliTestEnv();
        env.EnsureForestInitialized();
        env.WriteCustomPlanter(planterId: "p1");
        env.WritePlant(key: "plan-a:alpha", status: "planned", title: "Alpha");

        using var console = new ConsoleCapture();
        var exitCode = await CliApp.InvokeAsync(
            new[] { "planter", "p1", "grow", "plan-a:alpha", "--mode", "nope", "--json" }
        );

        Assert.That(exitCode, Is.EqualTo(ExitCodes.InvalidArguments));

        var doc = JsonDocument.Parse(console.StdOut);
        var err = doc.RootElement.GetProperty("error");
        Assert.That(err.GetProperty("code").GetString(), Is.EqualTo("invalid_arguments"));
        Assert.That(
            err.GetProperty("message").GetString(),
            Is.EqualTo("Invalid --mode. Expected: propose|apply")
        );
        Assert.That(err.GetProperty("details").GetProperty("mode").GetString(), Is.EqualTo("nope"));
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
        }

        public void WriteCustomPlanter(string planterId)
        {
            var plantersDir = Path.Combine(_forestDir, "planters", planterId);
            Directory.CreateDirectory(plantersDir);
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
