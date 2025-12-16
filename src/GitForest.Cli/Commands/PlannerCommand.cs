using System.CommandLine;
using System.CommandLine.Invocation;
using GitForest.Cli.Features.Planner;
using MediatR;

namespace GitForest.Cli.Commands;

public static class PlannerCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var plannerCommand = new Command("planner", "Manage a specific planner");
        var plannerIdArg = new Argument<string>("planner-id", "Planner identifier");
        plannerCommand.AddArgument(plannerIdArg);

        var runCommand = new Command("run", "Run planner");
        var planOption = new Option<string>("--plan", "Plan ID to run against") { IsRequired = true };
        runCommand.AddOption(planOption);

        runCommand.SetHandler(async (InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var plannerId = context.ParseResult.GetValueForArgument(plannerIdArg);
            var plan = context.ParseResult.GetValueForOption(planOption) ?? string.Empty;

            var result = await mediator.Send(new RunPlannerCommand(PlannerId: plannerId, PlanId: plan));

            if (output.Json)
            {
                output.WriteJson(new { plannerId = result.PlannerId, plan = result.PlanId, status = result.Status });
            }
            else
            {
                output.WriteLine($"Running planner '{result.PlannerId}' for plan '{result.PlanId}'...");
                output.WriteLine("done");
            }

            context.ExitCode = ExitCodes.Success;
        });

        plannerCommand.AddCommand(runCommand);
        return plannerCommand;
    }
}


