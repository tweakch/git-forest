var builder = DistributedApplication.CreateBuilder(args);

// Add Orleans clustering
var orleans = builder.AddOrleans("git-forest-cluster")
    .WithDevelopmentClustering();

// Add the GitForest Web application with Orleans support
// Note: Use ExecutablePath or ContainerName for the web app reference
// For now, we'll configure Orleans as a standalone service
var gitForestWeb = builder.AddProject("gitforest-web", "../GitForest.Web/GitForest.Web.csproj")
    .WithReference(orleans);

builder.Build().Run();
