# GitForest.Infrastructure.Distributed

This project provides distributed infrastructure for git-forest using Microsoft Orleans and .NET Aspire.

## Overview

The distributed infrastructure enables scalable, distributed execution of planners and planters across multiple nodes using the Orleans actor framework. This allows git-forest to handle large forests with many repositories and complex plans efficiently.

## Key Components

### Orleans Grains

- **IPlannerGrain / PlannerGrain**: Distributed planner execution
  - Deterministic generation of plants from plans + repository context
  - Parallel processing across multiple silo nodes
  - State tracking for planner execution status

- **IPlanterGrain / PlanterGrain**: Distributed planter execution
  - Executor personas that propose diffs/PRs for plants
  - Capacity management (default: 10 plants per planter)
  - Support for both "propose" and "apply" execution modes

### Service Registration

Use `ServiceCollectionExtensions` to register Orleans services:

```csharp
services.AddOrleansDistributedInfrastructure(config =>
{
    config.UseMemoryStorage = true;
    config.SiloPort = 11111;
    config.GatewayPort = 30000;
});
```

For client applications:

```csharp
services.AddOrleansClient(config =>
{
    // Configuration options
});
```

## Architecture

The distributed architecture follows git-forest's design goals:

- **Deterministic IDs**: Planners always produce the same plant keys for the same input
- **Idempotent**: Operations can be safely retried
- **Safe Concurrency**: Orleans grains provide built-in concurrency control
- **Clear Ownership**: Each planter has explicit capacity limits and assignments

## Integration with Aspire

The `GitForest.AppHost` project provides .NET Aspire orchestration:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var orleans = builder.AddOrleans("git-forest-cluster")
    .WithDevelopmentClustering();

var gitForestWeb = builder.AddProject("gitforest-web", "../GitForest.Web/GitForest.Web.csproj")
    .WithReference(orleans);

builder.Build().Run();
```

## Usage

### Running the Aspire AppHost

```bash
cd src/GitForest.AppHost
dotnet run
```

This will start:
- Orleans silo cluster
- GitForest.Web application with Orleans integration
- Aspire dashboard for monitoring

### Accessing Grains

```csharp
// Get a planner grain
var plannerGrain = grainFactory.GetGrain<IPlannerGrain>("my-planner-id");
var plants = await plannerGrain.ExecutePlannerAsync("sample", "/path/to/repo");

// Get a planter grain
var planterGrain = grainFactory.GetGrain<IPlanterGrain>("my-planter-id");
await planterGrain.AssignPlantAsync("sample:plant-1");
var result = await planterGrain.ExecutePlanterAsync("sample:plant-1", ExecutionMode.Propose);
```

## Configuration

### Development
Uses localhost clustering for development. No additional configuration required.

### Production
For production deployments, configure:
- Azure Storage clustering for multi-node scenarios
- Persistent grain storage
- Custom silo and gateway ports

## Future Enhancements

- [ ] Integrate with actual GitForest.Core planner/planter logic
- [ ] Add grain persistence for durable state
- [ ] Implement grain observers for real-time updates
- [ ] Add metrics and telemetry
- [ ] Support for custom clustering providers
- [ ] Grain call filters for logging and error handling
