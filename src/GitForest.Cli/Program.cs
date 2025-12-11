using System.CommandLine;
using System.CommandLine.Invocation;
using GitForest.Core;

var rootCommand = new RootCommand("git-forest - A .NET Aspire-based CLI for managing git repository forests");

// Init command
var initCommand = new Command("init", "Initialize a new forest in the current directory");
initCommand.SetHandler(() =>
{
    Console.WriteLine("Initializing forest...");
    Console.WriteLine("Forest initialized successfully!");
});
rootCommand.AddCommand(initCommand);

// Status command
var statusCommand = new Command("status", "Show the status of the current forest");
statusCommand.SetHandler(() =>
{
    Console.WriteLine("Forest Status:");
    Console.WriteLine("No plants found in the forest.");
});
rootCommand.AddCommand(statusCommand);

// Plant command
var plantCommand = new Command("plant", "Add a new plant (repository) to the forest");
var plantNameOption = new Option<string>("--name", "Name of the plant");
var plantPathOption = new Option<string>("--path", "Path to the repository");
plantCommand.AddOption(plantNameOption);
plantCommand.AddOption(plantPathOption);
plantCommand.SetHandler((string name, string path) =>
{
    Console.WriteLine($"Planting '{name}' at '{path}'...");
    Console.WriteLine("Plant added successfully!");
}, plantNameOption, plantPathOption);
rootCommand.AddCommand(plantCommand);

// Plants command
var plantsCommand = new Command("plants", "List all plants in the forest");
plantsCommand.SetHandler(() =>
{
    Console.WriteLine("Plants in the forest:");
    Console.WriteLine("No plants found.");
});
rootCommand.AddCommand(plantsCommand);

// Planter command
var planterCommand = new Command("planter", "Add or view a planter (contributor)");
var planterNameOption = new Option<string?>("--name", "Name of the planter");
var planterEmailOption = new Option<string?>("--email", "Email of the planter");
planterCommand.AddOption(planterNameOption);
planterCommand.AddOption(planterEmailOption);
planterCommand.SetHandler((string? name, string? email) =>
{
    if (name != null && email != null)
    {
        Console.WriteLine($"Adding planter '{name}' ({email})...");
        Console.WriteLine("Planter added successfully!");
    }
    else
    {
        Console.WriteLine("Current planter information:");
        Console.WriteLine("No planter configured.");
    }
}, planterNameOption, planterEmailOption);
rootCommand.AddCommand(planterCommand);

// Planters command
var plantersCommand = new Command("planters", "List all planters in the forest");
plantersCommand.SetHandler(() =>
{
    Console.WriteLine("Planters in the forest:");
    Console.WriteLine("No planters found.");
});
rootCommand.AddCommand(plantersCommand);

// Planner command
var plannerCommand = new Command("planner", "Add or view a planner (organizer/manager)");
var plannerNameOption = new Option<string?>("--name", "Name of the planner");
var plannerEmailOption = new Option<string?>("--email", "Email of the planner");
var plannerRoleOption = new Option<string?>("--role", "Role of the planner");
plannerCommand.AddOption(plannerNameOption);
plannerCommand.AddOption(plannerEmailOption);
plannerCommand.AddOption(plannerRoleOption);
plannerCommand.SetHandler((string? name, string? email, string? role) =>
{
    if (name != null && email != null)
    {
        Console.WriteLine($"Adding planner '{name}' ({email}) with role '{role ?? "organizer"}'...");
        Console.WriteLine("Planner added successfully!");
    }
    else
    {
        Console.WriteLine("Current planner information:");
        Console.WriteLine("No planner configured.");
    }
}, plannerNameOption, plannerEmailOption, plannerRoleOption);
rootCommand.AddCommand(plannerCommand);

// Planners command
var plannersCommand = new Command("planners", "List all planners in the forest");
plannersCommand.SetHandler(() =>
{
    Console.WriteLine("Planners in the forest:");
    Console.WriteLine("No planners found.");
});
rootCommand.AddCommand(plannersCommand);

return await rootCommand.InvokeAsync(args);
