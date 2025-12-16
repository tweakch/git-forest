using GitForest.Web.Components;
using GitForest.Core.Persistence;
using GitForest.Core.Services;
using GitForest.Infrastructure.FileSystem.Forest;
using GitForest.Infrastructure.FileSystem.Llm;
using GitForest.Infrastructure.FileSystem.Plans;
using GitForest.Infrastructure.FileSystem.Repositories;
using GitForest.Infrastructure.FileSystem.Catalog;
using GitForest.Infrastructure.Memory;
using GitForest.Application.Features.Plans;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure git-forest services - simplified for web app
var userHomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var forestDir = Path.Combine(userHomeDir, ".git-forest");

// Ensure forest is initialized
if (!Directory.Exists(forestDir))
{
    Directory.CreateDirectory(forestDir);
    Directory.CreateDirectory(Path.Combine(forestDir, "plans"));
    Directory.CreateDirectory(Path.Combine(forestDir, "plants"));
    Directory.CreateDirectory(Path.Combine(forestDir, "planters"));
    Directory.CreateDirectory(Path.Combine(forestDir, "planners"));
    Directory.CreateDirectory(Path.Combine(forestDir, "logs"));
}

// MediatR handlers
builder.Services.AddMediator(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(ListPlansQuery).Assembly);
});

// Forest lifecycle ports
builder.Services.AddSingleton<IForestInitializer>(_ => new FileSystemForestInitializer());
builder.Services.AddSingleton<ILockStatusProvider>(_ => new FileSystemLockStatusProvider(forestDir));
builder.Services.AddSingleton<IPlanInstaller>(_ => new FileSystemPlanInstaller(forestDir));
builder.Services.AddSingleton<IReconciliationForum>(_ => new FileSystemReconciliationForum(forestDir));
builder.Services.AddSingleton<IPlanReconciler, ForumPlanReconciler>();

// Catalog plan reader for config/plans directory
var repoRoot = Directory.GetCurrentDirectory();
var catalogPath = Path.Combine(repoRoot, "config", "plans");
builder.Services.AddSingleton<ICatalogPlanReader>(_ => new FileSystemCatalogPlanReader(catalogPath));

// Default LLM chat client (mock for offline determinism)
builder.Services.AddSingleton<IAgentChatClient>(_ => new DeterministicMockAgentChatClient(
    defaultModel: "gpt-4o-mini",
    defaultTemperature: 0));

// Repository ports (file-based persistence)
builder.Services.AddSingleton<IPlanRepository>(_ => new FileSystemPlanRepository(forestDir));
builder.Services.AddSingleton<IPlantRepository>(_ => new FileSystemPlantRepository(forestDir));
builder.Services.AddSingleton<IPlanterRepository>(_ => new FileSystemPlanterRepository(forestDir));
builder.Services.AddSingleton<IPlannerRepository>(_ => new FileSystemPlannerRepository(forestDir));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
