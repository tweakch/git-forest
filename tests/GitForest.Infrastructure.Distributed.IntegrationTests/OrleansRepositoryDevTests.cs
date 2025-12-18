using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Infrastructure.Distributed.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Orleans.Serialization;

namespace GitForest.Infrastructure.Distributed.IntegrationTests;

/// <summary>
/// Integration tests for Orleans-based repositories in development environment
/// </summary>
[TestFixture]
[Category("Orleans")]
[Category("Integration")]
[Explicit("Orleans serialization configuration pending")]
public class OrleansRepositoryDevTests
{
    private IHost? _host;
    private IServiceProvider? _services;

    [SetUp]
    public async Task Setup()
    {
        // Create host with Orleans silo for testing
        var builder = Host.CreateDefaultBuilder();

        builder.UseOrleans(
            (context, siloBuilder) =>
            {
                siloBuilder.UseLocalhostClustering();
                siloBuilder.AddMemoryGrainStorage("Default");
            }
        );

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
    public async Task Plan_AddAndRetrieve_ShouldWork()
    {
        // Arrange
        var repository = _services!.GetRequiredService<IPlanRepository>();
        var plan = new Plan
        {
            Id = "test-plan-dev",
            Version = "1.0.0",
            Author = "Test Author",
        };

        // Act
        await repository.AddAsync(plan);
        var retrieved = await repository.GetByIdAsync("test-plan-dev");

        // Assert
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Id, Is.EqualTo("test-plan-dev"));
        Assert.That(retrieved.Version, Is.EqualTo("1.0.0"));
        Assert.That(retrieved.Author, Is.EqualTo("Test Author"));
    }

    [Test]
    public async Task Plant_AddUpdateDelete_ShouldWork()
    {
        // Arrange
        var repository = _services!.GetRequiredService<IPlantRepository>();
        var plant = new Plant
        {
            Key = "test-plan:test-plant-dev",
            Slug = "test-plant-dev",
            PlanId = "test-plan",
            Title = "Test Plant",
            Status = "planned",
        };

        // Act - Add
        await repository.AddAsync(plant);
        var retrieved = await repository.GetByIdAsync("test-plan:test-plant-dev");

        // Assert - Add
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Key, Is.EqualTo("test-plan:test-plant-dev"));

        // Act - Update
        retrieved.Status = "planted";
        await repository.UpdateAsync(retrieved);
        var updated = await repository.GetByIdAsync("test-plan:test-plant-dev");

        // Assert - Update
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Status, Is.EqualTo("planted"));

        // Act - Delete
        await repository.DeleteAsync(updated);
        var deleted = await repository.GetByIdAsync("test-plan:test-plant-dev");

        // Assert - Delete
        Assert.That(deleted, Is.Null);
    }

    [Test]
    public async Task Planter_ConcurrentAdd_ShouldHandleGracefully()
    {
        // Arrange
        var repository = _services!.GetRequiredService<IPlanterRepository>();
        var planter1 = new Planter
        {
            Id = "concurrent-planter-dev",
            Name = "Concurrent Planter",
            Type = "builtin",
        };
        var planter2 = new Planter
        {
            Id = "concurrent-planter-dev",
            Name = "Duplicate Planter",
            Type = "builtin",
        };

        // Act & Assert
        await repository.AddAsync(planter1);
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await repository.AddAsync(planter2)
        );
    }

    [Test]
    public async Task Planner_ListMultiple_ShouldWork()
    {
        // Arrange
        var repository = _services!.GetRequiredService<IPlannerRepository>();
        var planner1 = new Planner
        {
            Id = "planner-1-dev",
            Name = "Planner 1",
            PlanId = "plan-1",
        };
        var planner2 = new Planner
        {
            Id = "planner-2-dev",
            Name = "Planner 2",
            PlanId = "plan-2",
        };

        // Act
        await repository.AddAsync(planner1);
        await repository.AddAsync(planner2);

        var spec = new Ardalis.Specification.Specification<Planner>();
        var all = await repository.ListAsync(spec);

        // Assert
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(p => p.Id == "planner-1-dev"), Is.True);
        Assert.That(all.Any(p => p.Id == "planner-2-dev"), Is.True);
    }
}
