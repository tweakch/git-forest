using System.CommandLine;
using GitForest.Cli;
using GitForest.Cli.Features.Planner;
using MediatR;

namespace GitForest.Cli.Commands;

public static class PlannerCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var plannerCommand = new Command("planner", "Manage a specific planner");
        var plannerIdArg = new Argument<string>("planner-id")
        {
            Description = "Planner identifier"
        };
        plannerCommand.Arguments.Add(plannerIdArg);

        var runCommand = new Command("run", "Run planner");
        var planOption = new Option<string>("--plan")
        {
            Description = "Plan ID to run against",
            Required = true
        };
        runCommand.Options.Add(planOption);

        runCommand.SetAction(async (parseResult, token) =>
        {
            var output = parseResult.GetOutput(cliOptions);
            var plannerId = parseResult.GetValue(plannerIdArg);
            var plan = parseResult.GetValue(planOption) ?? string.Empty;

            var result = await mediator.Send(new RunPlannerCommand(PlannerId: plannerId, PlanId: plan), token);

            if (output.Json)
            {
                output.WriteJson(new { plannerId = result.PlannerId, plan = result.PlanId, status = result.Status });
            }
            else
            {
                output.WriteLine($"Running planner '{result.PlannerId}' for plan '{result.PlanId}'...");
                output.WriteLine("done");
            }

            return ExitCodes.Success;
        });

        plannerCommand.Subcommands.Add(runCommand);
        return plannerCommand;
    }
}


