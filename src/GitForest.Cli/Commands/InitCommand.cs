using System.CommandLine;
using System.Diagnostics;
using GitForest.Cli.Orleans;
using MediatR;
using AppForest = GitForest.Application.Features.Forest;

namespace GitForest.Cli.Commands;

public static class InitCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var command = new Command("init", "Initialize forest in current git repo");

        var forceOption = new Option<bool>("--force") { Description = "Force re-initialization" };
        var dirOption = new Option<string>("--dir")
        {
            Description = "Directory for forest state",
            DefaultValueFactory = _ => ".git-forest",
        };
        var hostingOption = new Option<string?>("--hosting")
        {
            Description =
                "Hosting model / persistence provider (orleans|file|memory). If omitted, prompts in interactive mode.",
        };
        var setupOption = new Option<bool>("--setup")
        {
            Description =
                "If using orleans and no local cluster is found, attempt local setup (install Aspire workload and start AppHost).",
        };
        var fallbackToOption = new Option<string?>("--fallback-to")
        {
            Description =
                "If using orleans and setup/connect fails, fall back to another provider (file|memory).",
        };

        command.Options.Add(forceOption);
        command.Options.Add(dirOption);
        command.Options.Add(hostingOption);
        command.Options.Add(setupOption);
        command.Options.Add(fallbackToOption);

        command.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var isInteractive =
                    !output.Json && !Console.IsInputRedirected && !Console.IsOutputRedirected;
                var force = parseResult.GetValue(forceOption);
                var dir = parseResult.GetValue(dirOption);
                var hosting = parseResult.GetValue(hostingOption);
                var setup = parseResult.GetValue(setupOption);
                var fallbackTo = parseResult.GetValue(fallbackToOption);

                var provider = NormalizeProvider(hosting);
                if (provider is null)
                {
                    if (!isInteractive)
                    {
                        // Automation-friendly default: avoid prompts and avoid requiring distributed infra.
                        provider = "file";
                    }
                    else
                    {
                        provider = PromptForProvider();
                    }
                }

                var result = await mediator.Send(
                    new AppForest.InitForestCommand(DirOptionValue: dir, Force: force),
                    token
                );

                // Persist chosen provider into .git-forest/config.yaml.
                var configPath = Path.Combine(result.ForestDirPath, "config.yaml");
                var effective = ForestConfigReader.ReadEffective(result.ForestDirPath);
                var updated = effective with { PersistenceProvider = provider };
                ForestConfigWriter.WriteConfigYaml(configPath, updated);

                if (string.Equals(provider, "orleans", StringComparison.OrdinalIgnoreCase))
                {
                    var (connected, error) = await TryConnectOrleansAsync(updated, token);
                    if (connected)
                    {
                        if (isInteractive)
                        {
                            var yes = PromptYesNo(
                                $"Found Orleans at {updated.Orleans.GatewayHost}:{updated.Orleans.GatewayPort} (clusterId={updated.Orleans.ClusterId}, serviceId={updated.Orleans.ServiceId}). Use it?",
                                defaultYes: true
                            );

                            if (!yes)
                            {
                                updated = updated with { PersistenceProvider = "file" };
                                ForestConfigWriter.WriteConfigYaml(configPath, updated);
                            }
                        }
                    }
                    else
                    {
                        if (!isInteractive)
                        {
                            if (output.Json)
                            {
                                output.WriteJsonError(
                                    code: "orleans_not_found",
                                    message: "Orleans not found locally",
                                    details: new
                                    {
                                        gatewayHost = updated.Orleans.GatewayHost,
                                        gatewayPort = updated.Orleans.GatewayPort,
                                        clusterId = updated.Orleans.ClusterId,
                                        serviceId = updated.Orleans.ServiceId,
                                        setupAvailable = true,
                                    }
                                );
                            }
                            else
                            {
                                output.WriteErrorLine(
                                    $"Orleans not found at {updated.Orleans.GatewayHost}:{updated.Orleans.GatewayPort}. Re-run with `--fallback-to file` or start AppHost and retry."
                                );
                            }

                            return ExitCodes.OrleansNotAvailable;
                        }

                        if (output.Json)
                        {
                            output.WriteJsonError(
                                code: "orleans_not_found",
                                message: "Orleans not found locally",
                                details: new
                                {
                                    gatewayHost = updated.Orleans.GatewayHost,
                                    gatewayPort = updated.Orleans.GatewayPort,
                                    clusterId = updated.Orleans.ClusterId,
                                    serviceId = updated.Orleans.ServiceId,
                                    setupAvailable = true,
                                }
                            );
                            return ExitCodes.OrleansNotAvailable;
                        }

                        output.WriteErrorLine(
                            $"Orleans not found at {updated.Orleans.GatewayHost}:{updated.Orleans.GatewayPort} (clusterId={updated.Orleans.ClusterId}, serviceId={updated.Orleans.ServiceId})."
                        );
                        if (!string.IsNullOrWhiteSpace(error))
                        {
                            output.WriteErrorLine($"Details: {error}");
                        }

                        var doSetup =
                            setup
                            || PromptYesNo(
                                "Set up local Orleans now? (installs Aspire workload and starts AppHost)",
                                defaultYes: true
                            );

                        if (doSetup)
                        {
                            var setupOk = await TrySetupLocalOrleansAsync(output, token);
                            if (setupOk)
                            {
                                var ready = await WaitForOrleansAsync(
                                    updated,
                                    TimeSpan.FromSeconds(20),
                                    token
                                );
                                if (!ready)
                                {
                                    output.WriteErrorLine(
                                        "Started AppHost, but Orleans still wasn’t connectable yet."
                                    );
                                }
                            }
                        }

                        var finalOk = await WaitForOrleansAsync(
                            updated,
                            TimeSpan.FromSeconds(3),
                            token
                        );
                        if (!finalOk)
                        {
                            var fallback = NormalizeProvider(fallbackTo);
                            if (fallback is "file" or "memory")
                            {
                                updated = updated with { PersistenceProvider = fallback };
                                ForestConfigWriter.WriteConfigYaml(configPath, updated);
                                output.WriteLine($"Initialized with fallback provider: {fallback}");
                            }
                            else
                            {
                                output.WriteErrorLine(
                                    "Orleans is required but not available. Start the AppHost (recommended: `aspire run`) and retry, or re-run init with `--fallback-to file`."
                                );
                                return ExitCodes.OrleansNotAvailable;
                            }
                        }
                    }
                }

                if (output.Json)
                {
                    output.WriteJson(
                        new
                        {
                            status = "initialized",
                            directory = result.DirectoryOptionValue,
                            path = result.ForestDirPath,
                            provider = updated.PersistenceProvider,
                        }
                    );
                }
                else
                {
                    output.WriteLine(
                        $"initialized ({result.DirectoryOptionValue}) provider={updated.PersistenceProvider}"
                    );
                }

                return ExitCodes.Success;
            }
        );

        return command;
    }

    private static string? NormalizeProvider(string? raw)
    {
        var p = (raw ?? string.Empty).Trim();
        if (p.Length == 0)
        {
            return null;
        }

        p = p.ToLowerInvariant();
        return p is "orleans" or "file" or "memory" ? p : null;
    }

    private static string PromptForProvider()
    {
        while (true)
        {
            Console.Write("Choose hosting model [orleans|file|memory] (default: orleans): ");
            var input = Console.ReadLine();
            var normalized = NormalizeProvider(input);
            if (normalized is null)
            {
                return "orleans";
            }

            return normalized;
        }
    }

    private static bool PromptYesNo(string prompt, bool defaultYes)
    {
        var suffix = defaultYes ? " [Y/n]" : " [y/N]";
        while (true)
        {
            Console.Write($"{prompt}{suffix}: ");
            var input = (Console.ReadLine() ?? string.Empty).Trim();
            if (input.Length == 0)
            {
                return defaultYes;
            }

            if (string.Equals(input, "y", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(input, "yes", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(input, "n", StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.Equals(input, "no", StringComparison.OrdinalIgnoreCase))
                return false;
        }
    }

    private static async Task<(bool connected, string? error)> TryConnectOrleansAsync(
        ForestConfig config,
        CancellationToken cancellationToken
    )
    {
        await using var accessor = new OrleansClientAccessor(config);
        try
        {
            await accessor.EnsureConnectedAsync(TimeSpan.FromSeconds(2), cancellationToken);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static async Task<bool> WaitForOrleansAsync(
        ForestConfig config,
        TimeSpan maxWait,
        CancellationToken cancellationToken
    )
    {
        var deadline = DateTime.UtcNow + maxWait;
        while (DateTime.UtcNow < deadline)
        {
            var (ok, _) = await TryConnectOrleansAsync(config, cancellationToken);
            if (ok)
            {
                return true;
            }

            await Task.Delay(500, cancellationToken);
        }

        return false;
    }

    private static async Task<bool> TrySetupLocalOrleansAsync(
        Output output,
        CancellationToken cancellationToken
    )
    {
        // Local environment setup (v1): start the GitForest Aspire AppHost from this repo.
        // We intentionally keep this scope narrow for now.
        var appHostProject = Path.Combine(
            Environment.CurrentDirectory,
            "src",
            "GitForest.AppHost",
            "GitForest.AppHost.csproj"
        );

        if (!File.Exists(appHostProject))
        {
            output.WriteErrorLine("Local setup is only supported from the git-forest repository.");
            output.WriteErrorLine(
                "Could not find AppHost project at: src/GitForest.AppHost/GitForest.AppHost.csproj"
            );
            output.WriteErrorLine(
                "Fix: run `aspire run --project src/GitForest.AppHost/GitForest.AppHost.csproj` from the git-forest repo."
            );
            return false;
        }

        // Ensure Aspire CLI is available.
        var aspireOk = await RunProcessAsync(
            output,
            fileName: "aspire",
            arguments: "--version",
            waitForExit: true,
            cancellationToken: cancellationToken
        );
        if (!aspireOk)
        {
            output.WriteErrorLine(
                "Aspire CLI not found. Install it from https://aspire.dev and retry."
            );
            return false;
        }

        // Start AppHost via Aspire CLI. We intentionally don't wait: it should keep running.
        _ = await RunProcessAsync(
            output,
            fileName: "aspire",
            arguments: $"run --project \"{appHostProject}\"",
            waitForExit: false,
            cancellationToken: cancellationToken
        );

        return true;
    }

    private static async Task<bool> RunProcessAsync(
        Output output,
        string fileName,
        string arguments,
        bool waitForExit,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
            };

            var process = Process.Start(psi);
            if (process is null)
            {
                output.WriteErrorLine($"Failed to start process: {fileName} {arguments}");
                return false;
            }

            if (!waitForExit)
            {
                output.WriteLine($"Started: {fileName} {arguments} (pid {process.Id})");
                return true;
            }

            await process.WaitForExitAsync(cancellationToken);
            if (process.ExitCode != 0)
            {
                output.WriteErrorLine(
                    $"Command failed (exit {process.ExitCode}): {fileName} {arguments}"
                );
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            output.WriteErrorLine($"Failed to run: {fileName} {arguments}");
            output.WriteErrorLine($"Details: {ex.Message}");
            return false;
        }
    }
}
