using Microsoft.Extensions.Logging;
using Orleans;

namespace GitForest.Infrastructure.Distributed.Grains;

/// <summary>
/// Orleans grain implementation for distributed planter execution.
/// </summary>
public class PlanterGrain : Grain, IPlanterGrain
{
    private readonly ILogger<PlanterGrain> _logger;
    private readonly List<string> _assignedPlants = new();
    private bool _isRunning;
    private DateTime? _lastExecutionTime;
    private const int DefaultCapacityLimit = 10;

    public PlanterGrain(ILogger<PlanterGrain> logger)
    {
        _logger = logger;
    }

    public async Task<PlanterExecutionResult> ExecutePlanterAsync(string plantKey, ExecutionMode mode)
    {
        var planterId = this.GetPrimaryKeyString();
        _logger.LogInformation("Executing planter {PlanterId} for plant {PlantKey} in mode {Mode}", 
            planterId, plantKey, mode);

        _isRunning = true;

        try
        {
            // TODO: Integrate with actual planter logic from GitForest.Core
            // This is a placeholder implementation
            await Task.Delay(100); // Simulate work

            _lastExecutionTime = DateTime.UtcNow;

            var result = new PlanterExecutionResult(
                plantKey,
                Success: true,
                DiffOrPullRequestUrl: mode == ExecutionMode.Propose ? $"https://github.com/example/pr/{plantKey}" : null,
                ErrorMessage: null);

            _logger.LogInformation("Planter {PlanterId} completed execution for plant {PlantKey}", planterId, plantKey);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Planter {PlanterId} failed to execute plant {PlantKey}", planterId, plantKey);
            return new PlanterExecutionResult(plantKey, Success: false, null, ex.Message);
        }
        finally
        {
            _isRunning = false;
        }
    }

    public Task<PlanterStatus> GetStatusAsync()
    {
        var planterId = this.GetPrimaryKeyString();
        var status = new PlanterStatus(
            planterId, 
            _isRunning, 
            _assignedPlants.Count, 
            DefaultCapacityLimit, 
            _lastExecutionTime);
        return Task.FromResult(status);
    }

    public Task AssignPlantAsync(string plantKey)
    {
        var planterId = this.GetPrimaryKeyString();
        
        if (_assignedPlants.Count >= DefaultCapacityLimit)
        {
            throw new InvalidOperationException($"Planter {planterId} has reached capacity limit");
        }

        if (!_assignedPlants.Contains(plantKey))
        {
            _assignedPlants.Add(plantKey);
            _logger.LogInformation("Plant {PlantKey} assigned to planter {PlanterId}", plantKey, planterId);
        }

        return Task.CompletedTask;
    }
}
