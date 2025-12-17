using Microsoft.Extensions.Logging;

namespace GitForest.Infrastructure.Distributed.Grains;

/// <summary>
/// Orleans grain implementation for distributed planner execution.
/// </summary>
public class PlannerGrain : Grain, IPlannerGrain
{
    private readonly ILogger<PlannerGrain> _logger;
    private bool _isRunning;
    private DateTime? _lastExecutionTime;
    private int _plantCount;

    public PlannerGrain(ILogger<PlannerGrain> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> ExecutePlannerAsync(string planId, string repositoryPath)
    {
        var plannerId = this.GetPrimaryKeyString();
        _logger.LogInformation("Executing planner {PlannerId} for plan {PlanId} on repository {RepositoryPath}", 
            plannerId, planId, repositoryPath);

        _isRunning = true;

        try
        {
            // TODO: Integrate with actual planner logic from GitForest.Core
            // This is a placeholder implementation
            var plants = new List<string>
            {
                $"{planId}:plant-1",
                $"{planId}:plant-2"
            };

            _plantCount = plants.Count;
            _lastExecutionTime = DateTime.UtcNow;

            _logger.LogInformation("Planner {PlannerId} generated {PlantCount} plants", plannerId, _plantCount);

            return plants;
        }
        finally
        {
            _isRunning = false;
        }
    }

    public Task<PlannerStatus> GetStatusAsync()
    {
        var plannerId = this.GetPrimaryKeyString();
        var status = new PlannerStatus(plannerId, _isRunning, _lastExecutionTime, _plantCount);
        return Task.FromResult(status);
    }
}
