// Aspire 13 validates dashboard + OTLP configuration very early.
// Ensure we have a sane local default so `aspire run` works out-of-the-box.
Environment.SetEnvironmentVariable(
    "ASPNETCORE_URLS",
    Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "https://localhost:18888"
);
Environment.SetEnvironmentVariable(
    "ASPIRE_ALLOW_UNSECURED_TRANSPORT",
    Environment.GetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT") ?? "true"
);
Environment.SetEnvironmentVariable(
    "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL",
    Environment.GetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL") ?? "http://localhost:4317"
);
Environment.SetEnvironmentVariable(
    "ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL",
    Environment.GetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL") ?? "http://localhost:4318"
);

var builder = DistributedApplication.CreateBuilder(args);

// Add Orleans cluster with memory storage for development
var orleans = builder
    .AddOrleans("gitforest-cluster")
    .WithDevelopmentClustering()
    .WithClusterId("gitforest")
    .WithServiceId("gitforest")
    .WithMemoryGrainStorage("Default");

// Host a local silo which serves GitForest grains.
builder.AddProject(
        name: "gitforest-silo",
        projectPath: "../GitForest.OrleansSilo/GitForest.OrleansSilo.csproj"
    )
    .WithReference(orleans);

builder.Build().Run();
