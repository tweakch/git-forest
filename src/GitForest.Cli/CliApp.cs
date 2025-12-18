using System.CommandLine;
using GitForest.Application.Features.Plans;
using GitForest.Application.Features.Plants;
using GitForest.Cli.Commands;
using GitForest.Cli.Reconciliation;
using GitForest.Core.Persistence;
using GitForest.Core.Services;
using GitForest.Infrastructure.FileSystem.Forest;
using GitForest.Infrastructure.FileSystem.Llm;
using GitForest.Infrastructure.FileSystem.Plans;
using GitForest.Infrastructure.FileSystem.Repositories;
using GitForest.Infrastructure.Memory;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GitForest.Cli;

public static class CliApp
{
    public static Task<int> InvokeAsync(string[] args)
    {
        var options = new CliOptions();

        using var serviceProvider = BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var rootCommand = BuildRootCommand(options, mediator);
        var parseResult = rootCommand.Parse(args);
        return parseResult.InvokeAsync();
    }

    public static RootCommand BuildRootCommand(CliOptions options, IMediator mediator)
    {
        var rootCommand = new RootCommand("git-forest (gf) - CLI for managing repository forests");
        // Equivalent to AddGlobalOption in older versions.
        options.Json.Recursive = true;
        rootCommand.Options.Add(options.Json);

        rootCommand.Subcommands.Add(InitCommand.Build(options, mediator));
        rootCommand.Subcommands.Add(StatusCommand.Build(options, mediator));
        rootCommand.Subcommands.Add(ConfigCommand.Build(options, mediator));
        rootCommand.Subcommands.Add(PlansCommand.Build(options, mediator));
        rootCommand.Subcommands.Add(PlanCommand.Build(options, mediator));
        rootCommand.Subcommands.Add(PlantsCommand.Build(options, mediator));
        rootCommand.Subcommands.Add(PlantCommand.Build(options, mediator));
        rootCommand.Subcommands.Add(PlantersCommand.Build(options, mediator));
        rootCommand.Subcommands.Add(PlanterCommand.Build(options, mediator));
        rootCommand.Subcommands.Add(PlannersCommand.Build(options, mediator));
        rootCommand.Subcommands.Add(PlannerCommand.Build(options, mediator));

        return rootCommand;
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
        var forestConfig = ForestConfigReader.ReadEffective(forestDir);
        services.AddSingleton(forestConfig);

        // MediatR handlers live across multiple assemblies during migration.
        services.AddMediator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CliApp).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(ListPlansQuery).Assembly);
        });

        // Repository ports (swappable persistence). Default is file for backward compatibility.
        var provider = string.IsNullOrWhiteSpace(forestConfig.PersistenceProvider)
            ? ForestConfigReader.DefaultPersistenceProvider
            : forestConfig.PersistenceProvider.Trim().ToLowerInvariant();

        // Forest lifecycle ports (filesystem-backed for now).
        services.AddSingleton<IForestInitializer>(_ => new FileSystemForestInitializer());
        services.AddSingleton<ILockStatusProvider>(_ => new FileSystemLockStatusProvider(
            forestDir
        ));
        services.AddSingleton<IPlanInstaller>(_ => new FileSystemPlanInstaller(forestDir));
        services.AddSingleton<FileSystemReconciliationForum>(_ => new FileSystemReconciliationForum(
            forestDir
        ));
        services.AddSingleton<AgentReconciliationForum>();
        services.AddSingleton<IReconciliationForumRouter>(sp => new ReconciliationForumRouter(
            config: sp.GetRequiredService<ForestConfig>(),
            fileForum: sp.GetRequiredService<FileSystemReconciliationForum>(),
            aiForum: sp.GetRequiredService<AgentReconciliationForum>()
        ));
        services.AddSingleton<IPlanReconciler, ForumPlanReconciler>();

        // LLM / agent chat client (default mock for offline determinism).
        var llmProvider = string.IsNullOrWhiteSpace(forestConfig.Llm.Provider)
            ? ForestConfigReader.DefaultLlmProvider
            : forestConfig.Llm.Provider.Trim().ToLowerInvariant();

        switch (llmProvider)
        {
            case "openai":
            case "ollama":
                services.AddSingleton<IAgentChatClient>(_ => new OpenAiCompatibleAgentChatClient(
                    httpClient: new HttpClient(),
                    baseUrl: string.IsNullOrWhiteSpace(forestConfig.Llm.BaseUrl)
                        ? ForestConfigReader.DefaultLlmBaseUrl
                        : forestConfig.Llm.BaseUrl.Trim(),
                    apiKeyEnvVar: string.IsNullOrWhiteSpace(forestConfig.Llm.ApiKeyEnvVar)
                        ? ForestConfigReader.DefaultLlmApiKeyEnvVar
                        : forestConfig.Llm.ApiKeyEnvVar.Trim(),
                    defaultModel: string.IsNullOrWhiteSpace(forestConfig.Llm.Model)
                        ? ForestConfigReader.DefaultLlmModel
                        : forestConfig.Llm.Model.Trim(),
                    defaultTemperature: forestConfig.Llm.Temperature
                ));
                break;
            case "mock":
            default:
                services.AddSingleton<IAgentChatClient>(_ => new DeterministicMockAgentChatClient(
                    defaultModel: string.IsNullOrWhiteSpace(forestConfig.Llm.Model)
                        ? ForestConfigReader.DefaultLlmModel
                        : forestConfig.Llm.Model.Trim(),
                    defaultTemperature: forestConfig.Llm.Temperature
                ));
                break;
        }

        switch (provider)
        {
            case "memory":
                services.AddSingleton<IPlanRepository>(_ => new InMemoryPlanRepository());
                services.AddSingleton<IPlantRepository>(_ => new InMemoryPlantRepository());
                services.AddSingleton<IPlanterRepository>(_ => new InMemoryPlanterRepository());
                services.AddSingleton<IPlannerRepository>(_ => new InMemoryPlannerRepository());
                break;

            case "orleans":
                // Scaffold only: until Orleans infra exists, fall back to file for CLI usability.
                services.AddSingleton<IPlanRepository>(_ => new FileSystemPlanRepository(
                    forestDir
                ));
                services.AddSingleton<IPlantRepository>(_ => new FileSystemPlantRepository(
                    forestDir
                ));
                services.AddSingleton<IPlanterRepository>(_ => new FileSystemPlanterRepository(
                    forestDir
                ));
                services.AddSingleton<IPlannerRepository>(_ => new FileSystemPlannerRepository(
                    forestDir
                ));
                break;

            case "file":
            default:
                services.AddSingleton<IPlanRepository>(_ => new FileSystemPlanRepository(
                    forestDir
                ));
                services.AddSingleton<IPlantRepository>(_ => new FileSystemPlantRepository(
                    forestDir
                ));
                services.AddSingleton<IPlanterRepository>(_ => new FileSystemPlanterRepository(
                    forestDir
                ));
                services.AddSingleton<IPlannerRepository>(_ => new FileSystemPlannerRepository(
                    forestDir
                ));
                break;
        }

        return services.BuildServiceProvider();
    }
}
