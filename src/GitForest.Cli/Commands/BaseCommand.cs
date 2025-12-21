using System.Linq;
using GitForest.Core.Services;

namespace GitForest.Cli.Commands;

internal static class BaseCommand
{
    internal static int WriteForestNotInitialized(Output output)
    {
        if (output.Json)
        {
            output.WriteJsonError(code: "forest_not_initialized", message: "Forest not initialized");
        }
        else
        {
            output.WriteErrorLine("Error: forest not initialized");
        }

        return ExitCodes.ForestNotInitialized;
    }

    internal static int WritePlanNotFound(Output output, string planId)
    {
        if (output.Json)
        {
            output.WriteJsonError(
                code: "plan_not_found",
                message: "Plan not found",
                details: new { planId }
            );
        }
        else
        {
            output.WriteErrorLine($"Error: plan not found: {planId}");
        }

        return ExitCodes.PlanNotFound;
    }

    internal static int WritePlantNotFound(Output output, string selector)
    {
        if (output.Json)
        {
            output.WriteJsonError(
                code: "plant_not_found",
                message: "Plant not found",
                details: new { selector }
            );
        }
        else
        {
            output.WriteErrorLine($"Plant '{selector}': not found");
        }

        return ExitCodes.PlantNotFoundOrAmbiguous;
    }

    internal static int WritePlantAmbiguous(
        Output output,
        string selector,
        string[] matches,
        bool printMatches = false
    )
    {
        if (output.Json)
        {
            output.WriteJsonError(
                code: "plant_ambiguous",
                message: "Plant selector is ambiguous",
                details: new { selector, matches }
            );
        }
        else
        {
            output.WriteErrorLine($"Plant '{selector}': ambiguous; matched {matches.Length} plants");
            if (printMatches)
            {
                foreach (var key in matches.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    output.WriteErrorLine($"- {key}");
                }
            }
        }

        return ExitCodes.PlantNotFoundOrAmbiguous;
    }

    internal static int WritePlanterNotFound(Output output, string planterId)
    {
        if (output.Json)
        {
            output.WriteJsonError(
                code: "planter_not_found",
                message: "Planter not found",
                details: new { planterId }
            );
        }
        else
        {
            output.WriteErrorLine($"Planter '{planterId}': not found");
        }

        return ExitCodes.PlanterNotFound;
    }

    internal static int WriteInvalidArguments(Output output, string message, object? details)
    {
        if (output.Json)
        {
            output.WriteJsonError(code: "invalid_arguments", message: message, details: details);
        }
        else
        {
            output.WriteErrorLine($"Error: {message}");
        }

        return ExitCodes.InvalidArguments;
    }

    internal static int WriteInvalidState(Output output, string message, object? details)
    {
        if (output.Json)
        {
            output.WriteJsonError(code: "invalid_state", message: message, details: details);
        }
        else
        {
            output.WriteErrorLine($"Error: {message}");
        }

        return ExitCodes.InvalidArguments;
    }

    internal static int WriteConfirmationRequired(Output output, string message, object? details)
    {
        if (output.Json)
        {
            output.WriteJsonError(code: "confirmation_required", message: message, details: details);
        }
        else
        {
            output.WriteErrorLine("Error: confirmation required. Re-run with --yes.");
        }

        return ExitCodes.InvalidArguments;
    }

    internal static int WriteGitFailed(Output output, GitRunner.GitRunnerException ex)
    {
        if (output.Json)
        {
            output.WriteJsonError(
                code: "git_failed",
                message: ex.Message,
                details: new
                {
                    exitCode = ex.ExitCode,
                    stdout = ex.StdOut,
                    stderr = ex.StdErr,
                }
            );
        }
        else
        {
            output.WriteErrorLine($"Error: {ex.Message}");
            if (!string.IsNullOrWhiteSpace(ex.StdErr))
            {
                output.WriteErrorLine(ex.StdErr.Trim());
            }
        }

        return ExitCodes.GitOperationFailed;
    }

    internal static int WriteInvalidMode(Output output, string mode)
    {
        if (output.Json)
        {
            output.WriteJsonError(
                code: "invalid_arguments",
                message: "Invalid --mode. Expected: propose|apply",
                details: new { mode }
            );
        }
        else
        {
            output.WriteErrorLine("Error: invalid --mode. Expected: propose|apply");
        }

        return ExitCodes.InvalidArguments;
    }
}

