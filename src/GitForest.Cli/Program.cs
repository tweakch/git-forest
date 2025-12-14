using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using GitForest.Core;

var rootCommand = new RootCommand("git-forest (gf) - CLI for managing repository forests");

// Global --json option
var jsonOption = new Option<bool>("--json", "Output in JSON format");
rootCommand.AddGlobalOption(jsonOption);

// Init command
var initCommand = new Command("init", "Initialize forest in current git repo");
var forceOption = new Option<bool>("--force", "Force re-initialization");
var dirOption = new Option<string>("--dir", () => ".git-forest", "Directory for forest state");
initCommand.AddOption(forceOption);
initCommand.AddOption(dirOption);
initCommand.SetHandler((bool json, bool force, string dir) =>
{
    if (json)
    {
        Console.WriteLine("{\"status\":\"initialized\",\"directory\":\".git-forest\"}");
    }
    else
    {
        Console.WriteLine("initialized (.git-forest)");
    }
}, jsonOption, forceOption, dirOption);
rootCommand.AddCommand(initCommand);

// Status command
var statusCommand = new Command("status", "Show forest status");
statusCommand.SetHandler((bool json) =>
{
    if (json)
    {
        Console.WriteLine("{\"forest\":\"initialized\",\"repo\":\"origin/main\",\"plans\":0,\"plants\":0,\"planters\":0,\"lock\":\"free\"}");
    }
    else
    {
        Console.WriteLine("Forest: initialized  Repo: origin/main");
        Console.WriteLine("Plans: 0 installed");
        Console.WriteLine("Plants: planned 0 | planted 0 | growing 0 | harvestable 0 | harvested 0");
        Console.WriteLine("Planters: 0 available | 0 active");
        Console.WriteLine("Lock: free");
    }
}, jsonOption);
rootCommand.AddCommand(statusCommand);

// Config command
var configCommand = new Command("config", "Manage configuration");
var configShowCommand = new Command("show", "Show configuration");
var effectiveOption = new Option<bool>("--effective", "Show effective configuration");
configShowCommand.AddOption(effectiveOption);
configShowCommand.SetHandler((bool json, bool effective) =>
{
    if (json)
    {
        Console.WriteLine("{\"config\":{}}");
    }
    else
    {
        Console.WriteLine("Configuration: (empty)");
    }
}, jsonOption, effectiveOption);
configCommand.AddCommand(configShowCommand);
rootCommand.AddCommand(configCommand);

// Plans commands
var plansCommand = new Command("plans", "Manage plans");

var plansListCommand = new Command("list", "List installed plans");
plansListCommand.SetHandler((bool json) =>
{
    if (json)
    {
        Console.WriteLine("{\"plans\":[]}");
    }
    else
    {
        Console.WriteLine("No plans installed");
    }
}, jsonOption);
plansCommand.AddCommand(plansListCommand);

var plansInstallCommand = new Command("install", "Install a plan");
var sourceArg = new Argument<string>("source", "Plan source (GitHub slug, URL, or local path)");
plansInstallCommand.AddArgument(sourceArg);
plansInstallCommand.SetHandler((bool json, string source) =>
{
    if (json)
    {
        Console.WriteLine($"{{\"status\":\"installed\",\"source\":\"{source}\"}}");
    }
    else
    {
        Console.WriteLine($"Installed plan from: {source}");
    }
}, jsonOption, sourceArg);
plansCommand.AddCommand(plansInstallCommand);

rootCommand.AddCommand(plansCommand);

// Plan command (single plan operations)
var planCommand = new Command("plan", "Manage a specific plan");
var planIdArg = new Argument<string>("plan-id", "Plan identifier");
planCommand.AddArgument(planIdArg);

var planReconcileCommand = new Command("reconcile", "Reconcile plan to desired state");
var updateOption = new Option<bool>("--update", "Update plan before reconciling");
var dryRunOption = new Option<bool>("--dry-run", "Show what would be done without applying");
planReconcileCommand.AddOption(updateOption);
planReconcileCommand.AddOption(dryRunOption);
planReconcileCommand.SetHandler((bool json, string planId, bool update, bool dryRun) =>
{
    if (json)
    {
        Console.WriteLine($"{{\"planId\":\"{planId}\",\"status\":\"reconciled\",\"dryRun\":{dryRun.ToString().ToLower()}}}");
    }
    else
    {
        Console.WriteLine($"Reconciling plan '{planId}'...");
        Console.WriteLine("Planners: +0 ~0 -0");
        Console.WriteLine("Planters: +0 ~0 -0");
        Console.WriteLine("Plants:   +0 ~0 -0 (archived 0)");
        Console.WriteLine(dryRun ? "done (dry-run)" : "done");
    }
}, jsonOption, planIdArg, updateOption, dryRunOption);
planCommand.AddCommand(planReconcileCommand);

rootCommand.AddCommand(planCommand);

// Plants commands
var plantsCommand = new Command("plants", "Manage plants");

var plantsListCommand = new Command("list", "List plants");
var statusFilterOption = new Option<string?>("--status", "Filter by status (planned|planted|growing|harvestable|harvested|archived)");
var planFilterOption = new Option<string?>("--plan", "Filter by plan ID");
plantsListCommand.AddOption(statusFilterOption);
plantsListCommand.AddOption(planFilterOption);
plantsListCommand.SetHandler((bool json, string? status, string? plan) =>
{
    if (json)
    {
        Console.WriteLine("{\"plants\":[]}");
    }
    else
    {
        Console.WriteLine("Key                             Status   Title                         Plan   Planter");
        Console.WriteLine("No plants found");
    }
}, jsonOption, statusFilterOption, planFilterOption);
plantsCommand.AddCommand(plantsListCommand);

rootCommand.AddCommand(plantsCommand);

// Plant command (single plant operations)
var plantCommand = new Command("plant", "Manage a specific plant");
var selectorArg = new Argument<string>("selector", "Plant selector (key, slug, or P01)");
plantCommand.AddArgument(selectorArg);

var plantShowCommand = new Command("show", "Show plant details");
plantShowCommand.SetHandler((bool json, string selector) =>
{
    // TODO: Implement actual plant lookup
    // For now, this is a stub that shows the intended error format
    if (json)
    {
        Console.WriteLine($"{{\"selector\":\"{selector}\",\"status\":\"not_found\"}}");
    }
    else
    {
        Console.WriteLine($"Plant '{selector}': not found");
    }
    // TODO: Return exit code 12 when actual implementation is done
    // Environment.Exit(12); // Plant not found exit code
}, jsonOption, selectorArg);
plantCommand.AddCommand(plantShowCommand);

rootCommand.AddCommand(plantCommand);

// Planters commands
var plantersCommand = new Command("planters", "Manage planters");

var plantersListCommand = new Command("list", "List planters");
var builtinOption = new Option<bool>("--builtin", "Show only built-in planters");
var customOption = new Option<bool>("--custom", "Show only custom planters");
plantersListCommand.AddOption(builtinOption);
plantersListCommand.AddOption(customOption);
plantersListCommand.SetHandler((bool json, bool builtin, bool custom) =>
{
    if (json)
    {
        Console.WriteLine("{\"planters\":[]}");
    }
    else
    {
        Console.WriteLine("No planters configured");
    }
}, jsonOption, builtinOption, customOption);
plantersCommand.AddCommand(plantersListCommand);

rootCommand.AddCommand(plantersCommand);

// Planter command (single planter operations)
var planterCommand = new Command("planter", "Manage a specific planter");
var planterIdArg = new Argument<string>("planter-id", "Planter identifier");
planterCommand.AddArgument(planterIdArg);

var planterShowCommand = new Command("show", "Show planter details");
planterShowCommand.SetHandler((bool json, string planterId) =>
{
    // TODO: Implement actual planter lookup
    // For now, this is a stub that shows the intended error format
    if (json)
    {
        Console.WriteLine($"{{\"planterId\":\"{planterId}\",\"status\":\"not_found\"}}");
    }
    else
    {
        Console.WriteLine($"Planter '{planterId}': not found");
    }
    // TODO: Return exit code 13 when actual implementation is done
    // Environment.Exit(13); // Planter not found exit code
}, jsonOption, planterIdArg);
planterCommand.AddCommand(planterShowCommand);

rootCommand.AddCommand(planterCommand);

// Planners commands
var plannersCommand = new Command("planners", "Manage planners");

var plannersListCommand = new Command("list", "List planners");
var plannerPlanFilterOption = new Option<string?>("--plan", "Filter by plan ID");
plannersListCommand.AddOption(plannerPlanFilterOption);
plannersListCommand.SetHandler((bool json, string? plan) =>
{
    if (json)
    {
        Console.WriteLine("{\"planners\":[]}");
    }
    else
    {
        Console.WriteLine("No planners configured");
    }
}, jsonOption, plannerPlanFilterOption);
plannersCommand.AddCommand(plannersListCommand);

rootCommand.AddCommand(plannersCommand);

// Planner command (single planner operations)
var plannerCommand = new Command("planner", "Manage a specific planner");
var plannerIdArg = new Argument<string>("planner-id", "Planner identifier");
plannerCommand.AddArgument(plannerIdArg);

var plannerRunCommand = new Command("run", "Run planner");
var plannerPlanOption = new Option<string>("--plan", "Plan ID to run against") { IsRequired = true };
plannerRunCommand.AddOption(plannerPlanOption);
plannerRunCommand.SetHandler((bool json, string plannerId, string plan) =>
{
    if (json)
    {
        Console.WriteLine($"{{\"plannerId\":\"{plannerId}\",\"plan\":\"{plan}\",\"status\":\"completed\"}}");
    }
    else
    {
        Console.WriteLine($"Running planner '{plannerId}' for plan '{plan}'...");
        Console.WriteLine("done");
    }
}, jsonOption, plannerIdArg, plannerPlanOption);
plannerCommand.AddCommand(plannerRunCommand);

rootCommand.AddCommand(plannerCommand);

var parser = new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .UseVersionOption()
    .Build();

return await parser.InvokeAsync(args);
