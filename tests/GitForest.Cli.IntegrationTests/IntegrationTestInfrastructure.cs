using System.Diagnostics;
using System.Text;
using NUnit.Framework;

namespace GitForest.Cli.IntegrationTests;

internal sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);

internal static class TestEnvironments
{
    public static IReadOnlyDictionary<string, string> DotNet { get; } =
        new Dictionary<string, string>
        {
            ["DOTNET_NOLOGO"] = "1",
            ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
            ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1",
        };

    public static IReadOnlyDictionary<string, string> Git { get; } =
        new Dictionary<string, string>
        {
            ["GIT_TERMINAL_PROMPT"] = "0",
            ["GIT_CONFIG_NOSYSTEM"] = "1",
        };
}

internal static class RepoPaths
{
    public static string FindRepoRoot(string startDirectory)
    {
        var dir = new DirectoryInfo(startDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "GitForest.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate repo root containing GitForest.sln starting from '{startDirectory}'"
        );
    }
}

internal static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environmentVariables,
        TimeSpan timeout
    )
    {
        var sw = Stopwatch.StartNew();
        var commandLine = FormatCommandLine(fileName, arguments);
        TestContext.Progress.WriteLine($"[proc:start] {commandLine}\n  cwd: {workingDirectory}");

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        foreach (var arg in arguments)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        if (environmentVariables is not null)
        {
            foreach (var (key, value) in environmentVariables)
            {
                process.StartInfo.Environment[key] = value;
            }
        }

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
            throw new InvalidOperationException($"Failed to start process: {fileName}");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            { /* ignore */
            }
            sw.Stop();
            TestContext.Progress.WriteLine(
                $"[proc:timeout] {commandLine}\n  elapsedMs: {sw.ElapsedMilliseconds}\n"
            );
            throw new TimeoutException($"Process timed out after {timeout}: {commandLine}");
        }

        sw.Stop();

        var result = new ProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString());
        TestContext.Progress.WriteLine(
            $"[proc:end] exit={result.ExitCode} elapsedMs={sw.ElapsedMilliseconds}\n"
        );
        return result;
    }

    private static string FormatCommandLine(string fileName, IReadOnlyList<string> arguments)
    {
        var sb = new StringBuilder(fileName);
        foreach (var arg in arguments)
        {
            sb.Append(' ');
            sb.Append(QuoteIfNeeded(arg));
        }

        return sb.ToString();
    }

    private static string QuoteIfNeeded(string arg)
    {
        if (arg.Length == 0)
        {
            return "\"\"";
        }

        // Rough formatting for logs only.
        var needsQuotes = arg.Any(char.IsWhiteSpace) || arg.Contains('"', StringComparison.Ordinal);
        if (!needsQuotes)
        {
            return arg;
        }

        return "\"" + arg.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }
}

internal static class GitRepo
{
    public static async Task CreateAsync(
        string directory,
        IReadOnlyDictionary<string, string> environmentVariables
    )
    {
        Directory.CreateDirectory(directory);

        await EnsureSuccessAsync(
            await ProcessRunner.RunAsync(
                "git",
                ["init"],
                directory,
                environmentVariables,
                TimeSpan.FromMinutes(1)
            )
        );
        await EnsureSuccessAsync(
            await ProcessRunner.RunAsync(
                "git",
                ["config", "user.email", "test@example.com"],
                directory,
                environmentVariables,
                TimeSpan.FromMinutes(1)
            )
        );
        await EnsureSuccessAsync(
            await ProcessRunner.RunAsync(
                "git",
                ["config", "user.name", "Test User"],
                directory,
                environmentVariables,
                TimeSpan.FromMinutes(1)
            )
        );
        // Ensure tests are not coupled to developer machine commit signing (e.g. 1Password SSH/GPG signing).
        await EnsureSuccessAsync(
            await ProcessRunner.RunAsync(
                "git",
                ["config", "commit.gpgsign", "false"],
                directory,
                environmentVariables,
                TimeSpan.FromMinutes(1)
            )
        );

        var readme = Path.Combine(directory, "README.md");
        await File.WriteAllTextAsync(readme, "# Test Repo\n", Encoding.UTF8);

        await EnsureSuccessAsync(
            await ProcessRunner.RunAsync(
                "git",
                ["add", "README.md"],
                directory,
                environmentVariables,
                TimeSpan.FromMinutes(1)
            )
        );
        await EnsureSuccessAsync(
            await ProcessRunner.RunAsync(
                "git",
                ["commit", "-m", "Initial commit"],
                directory,
                environmentVariables,
                TimeSpan.FromMinutes(1)
            )
        );
    }

    private static Task EnsureSuccessAsync(ProcessResult result)
    {
        Assert.That(
            result.ExitCode,
            Is.EqualTo(0),
            () => $"Git command failed.\nSTDOUT:\n{result.StdOut}\nSTDERR:\n{result.StdErr}"
        );
        return Task.CompletedTask;
    }
}

internal static class Git
{
    public static string AsText(string workingDirectory, IReadOnlyList<string> args)
    {
        var result = ProcessRunner
            .RunAsync(
                fileName: "git",
                arguments: args,
                workingDirectory: workingDirectory,
                environmentVariables: TestEnvironments.Git,
                timeout: TimeSpan.FromMinutes(1)
            )
            .GetAwaiter()
            .GetResult();

        Assert.That(
            result.ExitCode,
            Is.EqualTo(0),
            () => $"Git command failed.\nSTDOUT:\n{result.StdOut}\nSTDERR:\n{result.StdErr}"
        );
        return result.StdOut ?? string.Empty;
    }
}

internal static class GitForestCli
{
    private static readonly SemaphoreSlim BuildLock = new(1, 1);
    private static string? _cliDllPath;
    private static string? _builtConfiguration;
    private static string? _builtRepoRoot;

    public static async Task<ProcessResult> RunAsync(
        string workingDirectory,
        IReadOnlyList<string> args,
        TimeSpan timeout
    )
    {
        var cliDll = await GetCliDllPathAsync();
        var arguments = new List<string>(capacity: 1 + args.Count) { cliDll };
        arguments.AddRange(args);

        return await ProcessRunner.RunAsync(
            fileName: "dotnet",
            arguments: arguments,
            workingDirectory: workingDirectory,
            environmentVariables: TestEnvironments.DotNet,
            timeout: timeout
        );
    }

    private static async Task<string> GetCliDllPathAsync()
    {
        var repoRoot = RepoPaths.FindRepoRoot(TestContext.CurrentContext.TestDirectory);
        var configuration = InferConfigurationFromTestDirectory(
            TestContext.CurrentContext.TestDirectory
        );

        if (
            _cliDllPath is not null
            && string.Equals(_builtRepoRoot, repoRoot, StringComparison.OrdinalIgnoreCase)
            && string.Equals(_builtConfiguration, configuration, StringComparison.OrdinalIgnoreCase)
            && File.Exists(_cliDllPath)
        )
        {
            return _cliDllPath;
        }

        await BuildLock.WaitAsync();
        try
        {
            if (
                _cliDllPath is not null
                && string.Equals(_builtRepoRoot, repoRoot, StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                    _builtConfiguration,
                    configuration,
                    StringComparison.OrdinalIgnoreCase
                )
                && File.Exists(_cliDllPath)
            )
            {
                return _cliDllPath;
            }

            var cliProject = Path.Combine(repoRoot, "src", "GitForest.Cli", "GitForest.Cli.csproj");
            Assert.That(
                File.Exists(cliProject),
                Is.True,
                () => $"Expected CLI project at: {cliProject}"
            );

            var build = await ProcessRunner.RunAsync(
                fileName: "dotnet",
                arguments: ["build", cliProject, "--configuration", configuration, "--nologo"],
                workingDirectory: repoRoot,
                environmentVariables: TestEnvironments.DotNet,
                timeout: TimeSpan.FromMinutes(5)
            );

            Assert.That(
                build.ExitCode,
                Is.EqualTo(0),
                () => $"dotnet build failed.\nSTDOUT:\n{build.StdOut}\nSTDERR:\n{build.StdErr}"
            );

            var cliDir = Path.Combine(
                repoRoot,
                "src",
                "GitForest.Cli",
                "bin",
                configuration,
                "net10.0"
            );
            var cliDll = Path.Combine(cliDir, "GitForest.Cli.dll");
            Assert.That(File.Exists(cliDll), Is.True, () => $"Expected built CLI dll at: {cliDll}");

            _builtRepoRoot = repoRoot;
            _builtConfiguration = configuration;
            _cliDllPath = cliDll;

            return cliDll;
        }
        finally
        {
            BuildLock.Release();
        }
    }

    private static string InferConfigurationFromTestDirectory(string testDirectory)
    {
        // Typical values: .../bin/Debug/net10.0 or .../bin/Release/net10.0
        var normalized = testDirectory.Replace('\\', '/');
        if (normalized.Contains("/bin/Debug/", StringComparison.OrdinalIgnoreCase))
        {
            return "Debug";
        }

        if (normalized.Contains("/bin/Release/", StringComparison.OrdinalIgnoreCase))
        {
            return "Release";
        }

        // Default for CI runs, and still fine locally.
        return "Release";
    }
}

internal static class ToolPaths
{
    public static string GetToolCommandPath(string toolPathDirectory, string toolCommandName)
    {
        var fileName = toolCommandName;
        if (OperatingSystem.IsWindows())
        {
            fileName += ".exe";
        }

        return Path.Combine(toolPathDirectory, fileName);
    }
}
