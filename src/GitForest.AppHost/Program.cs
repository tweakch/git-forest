var builder = DistributedApplication.CreateBuilder(args);

// Add Orleans cluster with memory storage for development
var orleans = builder
    .AddOrleans("gitforest-cluster")
    .WithDevelopmentClustering()
    .WithMemoryGrainStorage("Default");

builder.Build().Run();
