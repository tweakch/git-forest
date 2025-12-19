using System.Text.Json;
using NUnit.Framework;

namespace GitForest.Cli.IntegrationTests;

internal static class CliTestAsserts
{
    public static void ExitCodeIs(ProcessResult result, int expectedExitCode, string context)
    {
        Assert.That(
            result.ExitCode,
            Is.EqualTo(expectedExitCode),
            () => FormatFailure(context, result)
        );
    }

    public static void Succeeded(ProcessResult result, string context) =>
        ExitCodeIs(result, expectedExitCode: 0, context);

    public static JsonDocument ParseJsonFromStdOut(ProcessResult result, string context)
    {
        var trimmed = result.StdOut.Trim();
        Assert.That(trimmed, Is.Not.Empty, () => $"{context}: expected JSON on stdout but stdout was empty.");
        try
        {
            return JsonDocument.Parse(trimmed);
        }
        catch (JsonException ex)
        {
            Assert.Fail($"{context}: stdout was not valid JSON.\nSTDOUT:\n{result.StdOut}\nParse error: {ex}");
            throw;
        }
    }

    private static string FormatFailure(string context, ProcessResult result) =>
        $"{context}\nexit={result.ExitCode}\nSTDOUT:\n{result.StdOut}\nSTDERR:\n{result.StdErr}";
}

