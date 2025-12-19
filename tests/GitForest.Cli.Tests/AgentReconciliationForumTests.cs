using GitForest.Core;
using GitForest.Core.Services;
using GitForest.Infrastructure.FileSystem.Plans;
using NUnit.Framework;

namespace GitForest.Cli.Tests;

[TestFixture]
public sealed class AgentReconciliationForumTests
{
    [Test]
    public async Task RunAsync_validJson_returnsDesiredPlants()
    {
        var chat = new StubChatClient(_ =>
            "{\"desiredPlants\":[{\"slug\":\"alpha\",\"title\":\"Alpha\",\"description\":\"Desc\",\"assignedPlanters\":[\"p1\"]},{\"slug\":\"beta\",\"title\":\"Beta\"}],\"summary\":\"ok\"}"
        );

        var forum = new AgentReconciliationForum(chat);
        var context = new ReconcileContext(
            PlanId: "plan",
            Plan: new Plan
            {
                Id = "plan",
                Planners = new List<string> { "planner-1" },
            },
            RawPlanYaml: null,
            ExistingPlants: Array.Empty<Plant>(),
            Repository: "org/repo"
        );

        var result = await forum.RunAsync(context);

        Assert.That(result.DesiredPlants, Has.Count.EqualTo(2));
        Assert.That(result.DesiredPlants[0].Key, Does.StartWith("plan:"));
        Assert.That(
            result.DesiredPlants.Select(p => p.Key),
            Is.EquivalentTo(new[] { "plan:alpha", "plan:beta" })
        );
        Assert.That(
            result.DesiredPlants.Select(p => p.PlannerId),
            Is.EquivalentTo(new[] { "planner-1", "planner-1" })
        );

        var alpha = result.DesiredPlants.Single(x => x.Slug == "alpha");
        Assert.That(alpha.Title, Is.EqualTo("Alpha"));
        Assert.That(alpha.Description, Is.EqualTo("Desc"));
        Assert.That(alpha.AssignedPlanters, Is.EquivalentTo(new[] { "p1" }));
    }

    [Test]
    public async Task RunAsync_invalidJson_returnsEmptyAndMetadataMarksInvalid()
    {
        var chat = new StubChatClient(_ => "not-json");
        var forum = new AgentReconciliationForum(chat);

        var context = new ReconcileContext(
            PlanId: "plan",
            Plan: new Plan
            {
                Id = "plan",
                Planners = new List<string> { "planner-1" },
            },
            RawPlanYaml: null,
            ExistingPlants: Array.Empty<Plant>(),
            Repository: null
        );

        var result = await forum.RunAsync(context);

        Assert.That(result.DesiredPlants, Is.Empty);
        Assert.That(result.Metadata, Is.Not.Null);
        Assert.That(
            result.Metadata!.TryGetValue("planner.planner-1.status", out var status),
            Is.True
        );
        Assert.That(status, Is.EqualTo("invalid").Or.EqualTo("error"));
    }

    [Test]
    public async Task RunAsync_multiplePlanners_dedupesSlugsDeterministically()
    {
        var chat = new StubChatClient(req =>
        {
            if (req.AgentId == "p1")
            {
                return "{\"desiredPlants\":[{\"slug\":\"same\",\"title\":\"FromP1\"},{\"slug\":\"a\",\"title\":\"A\"}]}";
            }

            return "{\"desiredPlants\":[{\"slug\":\"same\",\"title\":\"FromP2\"},{\"slug\":\"b\",\"title\":\"B\"}]}";
        });

        var forum = new AgentReconciliationForum(chat);
        var context = new ReconcileContext(
            PlanId: "plan",
            Plan: new Plan
            {
                Id = "plan",
                Planners = new List<string> { "p1", "p2" },
            },
            RawPlanYaml: null,
            ExistingPlants: Array.Empty<Plant>(),
            Repository: null
        );

        var result = await forum.RunAsync(context);

        Assert.That(
            result.DesiredPlants.Select(p => p.Slug),
            Is.EquivalentTo(new[] { "a", "b", "same" })
        );

        // Slug 'same' should be taken from the first planner in plan order.
        var same = result.DesiredPlants.Single(p => p.Slug == "same");
        Assert.That(same.PlannerId, Is.EqualTo("p1"));
        Assert.That(same.Title, Is.EqualTo("FromP1"));
    }

    [Test]
    public async Task RunAsync_jsonFences_areHandled()
    {
        var chat = new StubChatClient(_ =>
            "```json\n{\"desiredPlants\":[{\"slug\":\"alpha\",\"title\":\"Alpha\"}]}\n```"
        );

        var forum = new AgentReconciliationForum(chat);
        var context = new ReconcileContext(
            PlanId: "plan",
            Plan: new Plan
            {
                Id = "plan",
                Planners = new List<string> { "planner-1" },
            },
            RawPlanYaml: null,
            ExistingPlants: Array.Empty<Plant>(),
            Repository: null
        );

        var result = await forum.RunAsync(context);

        Assert.That(
            result.DesiredPlants.Select(p => p.Key),
            Is.EquivalentTo(new[] { "plan:alpha" })
        );
    }

    private sealed class StubChatClient : IAgentChatClient
    {
        private readonly Func<AgentChatRequest, string> _getResponse;

        public StubChatClient(Func<AgentChatRequest, string> getResponse)
        {
            _getResponse = getResponse ?? throw new ArgumentNullException(nameof(getResponse));
        }

        public Task<AgentChatResponse> ChatAsync(
            AgentChatRequest request,
            CancellationToken cancellationToken = default
        )
        {
            _ = cancellationToken;
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            var json = _getResponse(request);
            return Task.FromResult(new AgentChatResponse(RawContent: json, Json: json));
        }
    }
}
