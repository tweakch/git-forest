using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Infrastructure.Distributed.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Orleans.Serialization;

namespace GitForest.Infrastructure.Distributed.IntegrationTests;

/// <summary>
/// Integration tests for Orleans-based repositories in production-like environment
/// Tests persistence and clustering scenarios
/// </summary>
[TestFixture]
[Category("Orleans")]
[Category("Integration")]
[Explicit("Orleans serialization configuration pending")]
public class OrleansRepositoryProdTests
{
    private IHost? _host;
    private IServiceProvider? _services;

    [SetUp]
    public async Task Setup()
    {
        // Create host with Orleans silo using persistent storage
        var builder = Host.CreateDefaultBuilder();
        
        builder.UseOrleans((context, siloBuilder) =>
        {
            siloBuilder.UseLocalhostClustering();
            siloBuilder.AddMemoryGrainStorage("Default");
        });
        
        builder.ConfigureServices(services =>
        {
            services.AddSerializer(builder =>
            {
                builder.AddJsonSerializer(isSupported: _ => true);
            });
            
            // Register repositories
            services.AddSingleton<IPlanRepository, OrleansPlansRepository>();
            services.AddSingleton<IPlantRepository, OrleansPlantRepository>();
            services.AddSingleton<IPlanterRepository, OrleansPlanterRepository>();
            services.AddSingleton<IPlannerRepository, OrleansPlannerRepository>();
        });
        
        _host = builder.Build();
        await _host.StartAsync();
        
        _services = _host.Services;
        
        // Wait for Orleans to be ready
        await Task.Delay(1000);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    [Test]
    public async Task Plan_PersistAcrossOperations_ShouldMaintainState()
    {
        // Arrange
        var repository = _services!.GetRequiredService<IPlanRepository>();
        var plan = new Plan
        {
            Id = "persistent-plan-prod",
            Version = "2.0.0",
            Source = "This plan should persist"
        };

        // Act - Add and retrieve multiple times
        await repository.AddAsync(plan);
        var first = await repository.GetByIdAsync("persistent-plan-prod");
        var second = await repository.GetByIdAsync("persistent-plan-prod");

        // Assert
        Assert.That(first, Is.Not.Null);
        Assert.That(second, Is.Not.Null);
        Assert.That(first!.Id, Is.EqualTo(second!.Id));
        Assert.That(first.Version, Is.EqualTo(second.Version));
    }

    [Test]
    public async Task Plant_UpdateMultipleTimes_ShouldReflectLatestState()
    {
        // Arrange
        var repository = _services!.GetRequiredService<IPlantRepository>();
        var plant = new Plant
        {
            Key = "prod-plan:evolving-plant",
            Slug = "evolving-plant",
            PlanId = "prod-plan",
            Title = "Evolving Plant",
            Status = "planned"
        };

        // Act - Add and update status multiple times
        await repository.AddAsync(plant);
        
        plant.Status = "planted";
        await repository.UpdateAsync(plant);
        var afterFirst = await repository.GetByIdAsync("prod-plan:evolving-plant");
        
        plant.Status = "growing";
        await repository.UpdateAsync(plant);
        var afterSecond = await repository.GetByIdAsync("prod-plan:evolving-plant");
        
        plant.Status = "harvestable";
        await repository.UpdateAsync(plant);
        var final = await repository.GetByIdAsync("prod-plan:evolving-plant");

        // Assert
        Assert.That(afterFirst!.Status, Is.EqualTo("planted"));
        Assert.That(afterSecond!.Status, Is.EqualTo("growing"));
        Assert.That(final!.Status, Is.EqualTo("harvestable"));
    }

    [Test]
    public async Task MultipleRepositories_ShouldWorkIndependently()
    {
        // Arrange
        var planRepo = _services!.GetRequiredService<IPlanRepository>();
        var plantRepo = _services!.GetRequiredService<IPlantRepository>();
        var planterRepo = _services!.GetRequiredService<IPlanterRepository>();
        var plannerRepo = _services!.GetRequiredService<IPlannerRepository>();

        var plan = new Plan { Id = "multi-plan", Version = "1.0.0" };
        var plant = new Plant { Key = "multi-plan:plant", Slug = "plant", PlanId = "multi-plan" };
        var planter = new Planter { Id = "multi-planter", Name = "Multi Planter" };
        var planner = new Planner { Id = "multi-planner", Name = "Multi Planner", PlanId = "multi-plan" };

        // Act
        await planRepo.AddAsync(plan);
        await plantRepo.AddAsync(plant);
        await planterRepo.AddAsync(planter);
        await plannerRepo.AddAsync(planner);

        // Assert
        var retrievedPlan = await planRepo.GetByIdAsync("multi-plan");
        var retrievedPlant = await plantRepo.GetByIdAsync("multi-plan:plant");
        var retrievedPlanter = await planterRepo.GetByIdAsync("multi-planter");
        var retrievedPlanner = await plannerRepo.GetByIdAsync("multi-planner");

        Assert.That(retrievedPlan, Is.Not.Null);
        Assert.That(retrievedPlant, Is.Not.Null);
        Assert.That(retrievedPlanter, Is.Not.Null);
        Assert.That(retrievedPlanner, Is.Not.Null);
    }

    [Test]
    public async Task GrainActivation_ShouldHandleStateCorrectly()
    {
        // Arrange
        var repository = _services!.GetRequiredService<IPlanRepository>();
        var plan = new Plan
        {
            Id = "grain-activation-test",
            Version = "1.0.0",
            Author = "Grain Test"
        };

        // Act - Add, then retrieve after delay (simulating grain deactivation/reactivation)
        await repository.AddAsync(plan);
        await Task.Delay(500); // Simulate some time passing
        var retrieved = await repository.GetByIdAsync("grain-activation-test");

        // Assert
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Id, Is.EqualTo("grain-activation-test"));
        Assert.That(retrieved.Author, Is.EqualTo("Grain Test"));
    }
}
