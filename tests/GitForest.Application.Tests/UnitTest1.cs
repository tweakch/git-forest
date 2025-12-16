using GitForest.Application.Features.Plans;
using GitForest.Application.Features.Plants;
using GitForest.Application.Features.Plants.Commands;
using GitForest.Application.Features.Planters;
using GitForest.Application.Features.Planners;
using GitForest.Core;
using GitForest.Infrastructure.Memory;

namespace GitForest.Application.Tests;

[TestFixture]
public sealed class HandlerAndRepositoryTests
{
    [Test]
    public async Task ListPlants_returns_all_plants_sorted_by_key()
    {
        var repo = new InMemoryPlantRepository(new[]
        {
            new Plant { Key = "b:two", PlanId = "b", Status = "planned" },
            new Plant { Key = "a:one", PlanId = "a", Status = "planted" },
            new Plant { Key = "b:one", PlanId = "b", Status = "planned" }
        });

        var handler = new ListPlantsHandler(repo);

        var result = await handler.Handle(new ListPlantsQuery(Status: null, PlanId: null), CancellationToken.None);

        Assert.That(result.Select(p => p.Key), Is.EqualTo(new[] { "a:one", "b:one", "b:two" }));
    }

    [Test]
    public async Task ListPlants_filters_by_status_and_sorts()
    {
        var repo = new InMemoryPlantRepository(new[]
        {
            new Plant { Key = "p:a", PlanId = "p", Status = "planned" },
            new Plant { Key = "p:b", PlanId = "p", Status = "planted" },
            new Plant { Key = "p:c", PlanId = "p", Status = "planned" }
        });

        var handler = new ListPlantsHandler(repo);

        var result = await handler.Handle(new ListPlantsQuery(Status: "planned", PlanId: null), CancellationToken.None);

        Assert.That(result.Select(p => p.Key), Is.EqualTo(new[] { "p:a", "p:c" }));
    }

    [Test]
    public async Task ListPlants_filters_by_planId_and_status()
    {
        var repo = new InMemoryPlantRepository(new[]
        {
            new Plant { Key = "p1:a", PlanId = "p1", Status = "planned" },
            new Plant { Key = "p1:b", PlanId = "p1", Status = "planted" },
            new Plant { Key = "p2:a", PlanId = "p2", Status = "planned" }
        });

        var handler = new ListPlantsHandler(repo);

        var result = await handler.Handle(new ListPlantsQuery(Status: "planned", PlanId: "p1"), CancellationToken.None);

        Assert.That(result.Select(p => p.Key), Is.EqualTo(new[] { "p1:a" }));
    }

    [Test]
    public async Task GetPlantByKey_returns_matching_plant()
    {
        var repo = new InMemoryPlantRepository(new[]
        {
            new Plant { Key = "plan:slug", PlanId = "plan", Status = "planned" }
        });

        var handler = new GetPlantByKeyHandler(repo);

        var plant = await handler.Handle(new GetPlantByKeyQuery("  plan:slug  "), CancellationToken.None);

        Assert.That(plant, Is.Not.Null);
        Assert.That(plant!.Key, Is.EqualTo("plan:slug"));
    }

    [Test]
    public void InMemoryPlantRepository_AddAsync_throws_on_duplicate_key()
    {
        var repo = new InMemoryPlantRepository();

        Assert.That(async () =>
        {
            await repo.AddAsync(new Plant { Key = "p:x", PlanId = "p", Status = "planned" });
            await repo.AddAsync(new Plant { Key = "p:x", PlanId = "p", Status = "planned" });
        }, Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task ListPlans_returns_all_plans_sorted_by_id()
    {
        var repo = new InMemoryPlanRepository(new[]
        {
            new Plan { Id = "b", Version = "1" },
            new Plan { Id = "a", Version = "1" }
        });

        var handler = new ListPlansHandler(repo);

        var plans = await handler.Handle(new ListPlansQuery(), CancellationToken.None);

        Assert.That(plans.Select(p => p.Id), Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public async Task GetPlanById_returns_matching_plan()
    {
        var repo = new InMemoryPlanRepository(new[]
        {
            new Plan { Id = "sample", Version = "1" }
        });

        var handler = new GetPlanByIdHandler(repo);

        var plan = await handler.Handle(new GetPlanByIdQuery(" sample "), CancellationToken.None);

        Assert.That(plan, Is.Not.Null);
        Assert.That(plan!.Id, Is.EqualTo("sample"));
    }

    [Test]
    public async Task ListPlanters_includes_builtin_aggregated_from_plans()
    {
        var plans = new InMemoryPlanRepository(new[]
        {
            new Plan { Id = "p1", Planters = new List<string> { "alpha", "beta" } },
            new Plan { Id = "p2", Planters = new List<string> { "alpha" } }
        });
        var planters = new InMemoryPlanterRepository();

        var handler = new ListPlantersHandler(plans, planters);

        var rows = await handler.Handle(new ListPlantersQuery(IncludeBuiltin: true, IncludeCustom: false), CancellationToken.None);

        Assert.That(rows.Select(r => $"{r.Id}:{r.Kind}:{string.Join(",", r.Plans)}"), Is.EqualTo(new[]
        {
            "alpha:builtin:p1,p2",
            "beta:builtin:p1"
        }));
    }

    [Test]
    public async Task GetPlanter_prefers_builtin_when_present_in_plan()
    {
        var plans = new InMemoryPlanRepository(new[]
        {
            new Plan { Id = "p1", Planters = new List<string> { "alpha" } }
        });
        var planters = new InMemoryPlanterRepository(new[]
        {
            new Planter { Id = "alpha", Type = "custom", Origin = "user" }
        });

        var handler = new GetPlanterHandler(plans, planters);

        var result = await handler.Handle(new GetPlanterQuery("alpha"), CancellationToken.None);

        Assert.That(result.Exists, Is.True);
        Assert.That(result.Kind, Is.EqualTo("builtin"));
        Assert.That(result.Plans, Is.EqualTo(new[] { "p1" }));
    }

    [Test]
    public async Task ListPlanners_aggregates_from_plans_and_supports_plan_filter()
    {
        var plans = new InMemoryPlanRepository(new[]
        {
            new Plan { Id = "p1", Planners = new List<string> { "x", "y" } },
            new Plan { Id = "p2", Planners = new List<string> { "x" } }
        });

        var handler = new ListPlannersHandler(plans);

        var all = await handler.Handle(new ListPlannersQuery(PlanFilter: null), CancellationToken.None);
        Assert.That(all.Select(r => $"{r.Id}:{string.Join(",", r.Plans)}"), Is.EqualTo(new[]
        {
            "x:p1,p2",
            "y:p1"
        }));

        var filtered = await handler.Handle(new ListPlannersQuery(PlanFilter: "p2"), CancellationToken.None);
        Assert.That(filtered.Select(r => $"{r.Id}:{string.Join(",", r.Plans)}"), Is.EqualTo(new[]
        {
            "x:p2"
        }));
    }

    [Test]
    public async Task AssignPlanter_transitions_planned_to_planted_and_persists_when_not_dry_run()
    {
        var repo = new InMemoryPlantRepository(new[]
        {
            new Plant { Key = "p:a", PlanId = "p", Status = "planned", AssignedPlanters = new List<string>() }
        });

        var handler = new AssignPlanterToPlantHandler(repo);

        var updated = await handler.Handle(new AssignPlanterToPlantCommand(Selector: "p:a", PlanterId: "worker", DryRun: false), CancellationToken.None);

        Assert.That(updated.Status, Is.EqualTo("planted"));
        Assert.That(updated.AssignedPlanters, Does.Contain("worker"));

        var persisted = await repo.GetByIdAsync("p:a");
        Assert.That(persisted, Is.Not.Null);
        Assert.That(persisted!.Status, Is.EqualTo("planted"));
        Assert.That(persisted.AssignedPlanters, Does.Contain("worker"));
    }

    [Test]
    public async Task AssignPlanter_dry_run_does_not_persist_changes()
    {
        var original = new Plant { Key = "p:a", PlanId = "p", Status = "planned", AssignedPlanters = new List<string>() };
        var repo = new InMemoryPlantRepository(new[] { original });
        var handler = new AssignPlanterToPlantHandler(repo);

        var updated = await handler.Handle(new AssignPlanterToPlantCommand(Selector: "p:a", PlanterId: "worker", DryRun: true), CancellationToken.None);

        Assert.That(updated.Status, Is.EqualTo("planted"));
        Assert.That(updated.AssignedPlanters, Does.Contain("worker"));

        var persisted = await repo.GetByIdAsync("p:a");
        Assert.That(persisted, Is.Not.Null);
        Assert.That(persisted!.Status, Is.EqualTo("planned"));
        Assert.That(persisted.AssignedPlanters, Does.Not.Contain("worker"));
    }

    [Test]
    public async Task UnassignPlanter_removes_planter_and_persists_when_not_dry_run()
    {
        var repo = new InMemoryPlantRepository(new[]
        {
            new Plant { Key = "p:a", PlanId = "p", Status = "planted", AssignedPlanters = new List<string> { "worker", "other" } }
        });

        var handler = new UnassignPlanterFromPlantHandler(repo);

        var updated = await handler.Handle(new UnassignPlanterFromPlantCommand(Selector: "p:a", PlanterId: "worker", DryRun: false), CancellationToken.None);

        Assert.That(updated.AssignedPlanters, Does.Not.Contain("worker"));
        Assert.That(updated.AssignedPlanters, Does.Contain("other"));
    }

    [Test]
    public void Harvest_requires_harvestable_unless_force()
    {
        var repo = new InMemoryPlantRepository(new[]
        {
            new Plant { Key = "p:a", PlanId = "p", Status = "planted" }
        });

        var handler = new HarvestPlantHandler(repo);

        Assert.That(async () => await handler.Handle(new HarvestPlantCommand(Selector: "p:a", Force: false, DryRun: false), CancellationToken.None),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task Archive_requires_harvested_unless_force()
    {
        var repo = new InMemoryPlantRepository(new[]
        {
            new Plant { Key = "p:a", PlanId = "p", Status = "planted" }
        });

        var handler = new ArchivePlantHandler(repo);

        Assert.That(async () => await handler.Handle(new ArchivePlantCommand(Selector: "p:a", Force: false, DryRun: false), CancellationToken.None),
            Throws.TypeOf<InvalidOperationException>());

        var forced = await handler.Handle(new ArchivePlantCommand(Selector: "p:a", Force: true, DryRun: false), CancellationToken.None);
        Assert.That(forced.Status, Is.EqualTo("archived"));
    }

    [Test]
    public async Task Selector_PIndex_resolves_deterministically_by_key_order()
    {
        var repo = new InMemoryPlantRepository(new[]
        {
            new Plant { Key = "p:b", PlanId = "p", Status = "planned" },
            new Plant { Key = "p:a", PlanId = "p", Status = "planned" }
        });

        var handler = new AssignPlanterToPlantHandler(repo);

        var updated = await handler.Handle(new AssignPlanterToPlantCommand(Selector: "P01", PlanterId: "worker", DryRun: true), CancellationToken.None);

        Assert.That(updated.Key, Is.EqualTo("p:a"));
    }
}
