using System;
using System.IO;
using System.Threading.Tasks;
using GitForest.Cli;
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
            var exitCode = await CliApp.InvokeAsync(new[] { "--version" });

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
            var exitCode = await CliApp.InvokeAsync(new[] { "plant", "sample:missing", "show", "--json" });

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
            var exitCode = await CliApp.InvokeAsync(new[] { "planter", "missing-planter", "show", "--json" });

            Assert.That(exitCode, Is.EqualTo(ExitCodes.PlanterNotFound));
            Assert.That(buffer.ToString(), Does.Contain("planter_not_found"));
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}


