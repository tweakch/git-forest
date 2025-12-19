using System;
using System.IO;
using NUnit.Framework;

namespace GitForest.Cli.Tests;

public class CliSmokeTests
{
    [Test]
    public async Task VersionOption_PrintsVersion_And_ExitsZero()
    {
        var originalOut = Console.Out;
        var buffer = new StringWriter();
        Console.SetOut(buffer);

        try
        {
            var exitCode = await CliApp.InvokeAsync("--version");

            Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
            Assert.That(buffer.ToString(), Is.Not.Empty);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Test]
    public async Task PlantShow_ReturnsExitCode12_And_JsonError_WhenJsonEnabled()
    {
        var originalOut = Console.Out;
        var buffer = new StringWriter();
        Console.SetOut(buffer);

        try
        {
            var exitCode = await CliApp.InvokeAsync("plant", "sample:missing", "show", "--json");

            Assert.That(exitCode, Is.EqualTo(ExitCodes.PlantNotFoundOrAmbiguous));
            Assert.That(buffer.ToString(), Does.Contain("plant_not_found"));
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Test]
    public async Task PlanterShow_ReturnsExitCode13_And_JsonError_WhenJsonEnabled()
    {
        var originalOut = Console.Out;
        var buffer = new StringWriter();
        Console.SetOut(buffer);

        try
        {
            var exitCode = await CliApp.InvokeAsync("planter", "missing-planter", "show", "--json");

            Assert.That(exitCode, Is.EqualTo(ExitCodes.PlanterNotFound));
            Assert.That(buffer.ToString(), Does.Contain("planter_not_found"));
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Test]
    public async Task Evolve_ReturnsForestNotInitialized_WhenForestMissing()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var buffer = new StringWriter();
        Console.SetOut(buffer);
        Console.SetError(buffer);
        var originalCwd = Environment.CurrentDirectory;
        var workDir = Path.Combine(Path.GetTempPath(), "git-forest", "tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(workDir);
        Environment.CurrentDirectory = workDir;

        try
        {
            var exitCode = await CliApp.InvokeAsync("evolve");

            Assert.That(exitCode, Is.EqualTo(ExitCodes.ForestNotInitialized));
            Assert.That(buffer.ToString(), Does.Contain("forest not initialized"));
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Environment.CurrentDirectory = originalCwd;
            try
            {
                if (Directory.Exists(workDir))
                {
                    Directory.Delete(workDir, recursive: true);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    [Test]
    public async Task Evolve_ReturnsJsonForestNotInitialized_WhenJsonEnabled()
    {
        var originalOut = Console.Out;
        var buffer = new StringWriter();
        Console.SetOut(buffer);
        var originalCwd = Environment.CurrentDirectory;
        var workDir = Path.Combine(Path.GetTempPath(), "git-forest", "tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(workDir);
        Environment.CurrentDirectory = workDir;

        try
        {
            var exitCode = await CliApp.InvokeAsync("evolve", "--json");

            Assert.That(exitCode, Is.EqualTo(ExitCodes.ForestNotInitialized));
            Assert.That(buffer.ToString(), Does.Contain("\"code\":\"forest_not_initialized\""));
        }
        finally
        {
            Console.SetOut(originalOut);
            Environment.CurrentDirectory = originalCwd;
            try
            {
                if (Directory.Exists(workDir))
                {
                    Directory.Delete(workDir, recursive: true);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }
}
