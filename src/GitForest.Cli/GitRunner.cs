using System.Diagnostics;
using System.Text;

namespace GitForest.Cli;

internal static class GitRunner
{
    public sealed record GitResult(int ExitCode, string StdOut, string StdErr);

    public static GitResult Run(IReadOnlyList<string> arguments, string? workingDirectory = null)
    {
        if (arguments is null)
        {
            throw new ArgumentNullException(nameof(arguments));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? Environment.CurrentDirectory : workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Avoid interactive prompts in automation contexts.
        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";

        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdout.AppendLine(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stderr.AppendLine(e.Data);
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start git process.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return new GitResult(process.ExitCode, stdout.ToString(), stderr.ToString());
    }

    public static GitResult RunOrThrow(IReadOnlyList<string> arguments, string? workingDirectory = null)
    {
        var result = Run(arguments, workingDirectory);
        if (result.ExitCode != 0)
        {
            throw new GitRunnerException(arguments, result.ExitCode, result.StdOut, result.StdErr);
        }

        return result;
    }

    public static bool HasUncommittedChanges(string? workingDirectory = null)
    {
        var res = Run(["status", "--porcelain"], workingDirectory);
        return res.ExitCode == 0 && !string.IsNullOrWhiteSpace(res.StdOut);
    }

    public static string GetRepoRoot(string? workingDirectory = null)
    {
        var res = RunOrThrow(["rev-parse", "--show-toplevel"], workingDirectory);
        return (res.StdOut ?? string.Empty).Trim();
    }

    public static bool BranchExists(string branchName, string? workingDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            return false;
        }

        var res = Run(["show-ref", "--verify", $"refs/heads/{branchName.Trim()}"], workingDirectory);
        return res.ExitCode == 0;
    }

    public static void CheckoutBranch(string branchName, bool createIfMissing, string? workingDirectory = null)
    {
        var name = (branchName ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            throw new ArgumentException("Branch name must be provided.", nameof(branchName));
        }

        if (BranchExists(name, workingDirectory))
        {
            RunOrThrow(["checkout", name], workingDirectory);
            return;
        }

        if (!createIfMissing)
        {
            throw new InvalidOperationException($"Branch does not exist: {name}");
        }

        RunOrThrow(["checkout", "-b", name], workingDirectory);
    }

    public static void AddAll(string? workingDirectory = null)
    {
        RunOrThrow(["add", "-A"], workingDirectory);
    }

    public static void Commit(string message, string? workingDirectory = null)
    {
        var msg = (message ?? string.Empty).Trim();
        if (msg.Length == 0)
        {
            throw new ArgumentException("Commit message must be provided.", nameof(message));
        }

        RunOrThrow(["commit", "-m", msg], workingDirectory);
    }

    public sealed class GitRunnerException : Exception
    {
        public IReadOnlyList<string> Arguments { get; }
        public int ExitCode { get; }
        public string StdOut { get; }
        public string StdErr { get; }

        public GitRunnerException(IReadOnlyList<string> arguments, int exitCode, string stdOut, string stdErr)
            : base($"git {string.Join(' ', arguments)} failed with exit code {exitCode}")
        {
            Arguments = arguments;
            ExitCode = exitCode;
            StdOut = stdOut;
            StdErr = stdErr;
        }
    }
}

