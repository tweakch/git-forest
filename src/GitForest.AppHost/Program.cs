var builder = DistributedApplication.CreateBuilder(args);

// Add Orleans cluster with memory storage for development
var orleans = builder
    .AddOrleans("gitforest-cluster")
    .WithDevelopmentClustering()
    .WithClusterId("gitforest")
    .WithServiceId("gitforest")
    .WithMemoryGrainStorage("Default");

// Host a local silo which serves GitForest grains.
builder
    .AddProject(
        name: "gitforest-silo",
        projectPath: "../GitForest.OrleansSilo/GitForest.OrleansSilo.csproj"
    )
    .WithReference(orleans);

builder.Build().Run();
