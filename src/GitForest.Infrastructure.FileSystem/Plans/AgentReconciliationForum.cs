using System.Text;
using System.Text.Json;
using GitForest.Core;
using GitForest.Core.Services;

namespace GitForest.Infrastructure.FileSystem.Plans;

/// <summary>
/// AI-backed reconciliation forum. Calls one agent per planner id and aggregates desired plants deterministically.
/// </summary>
public sealed class AgentReconciliationForum : IReconciliationForum
{
    private readonly IAgentChatClient _chat;

    public AgentReconciliationForum(IAgentChatClient chat)
    {
        _chat = chat ?? throw new ArgumentNullException(nameof(chat));
    }

    public async Task<ReconciliationStrategy> RunAsync(
        ReconcileContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        if (string.IsNullOrWhiteSpace(context.PlanId))
        {
            return new ReconciliationStrategy(
                Array.Empty<DesiredPlant>(),
                Summary: "ai:empty-plan-id"
            );
        }

        var planId = context.PlanId.Trim();
        var planners = context.Plan?.Planners ?? new List<string>();
        if (planners.Count == 0)
        {
            return new ReconciliationStrategy(
                Array.Empty<DesiredPlant>(),
                Summary: "ai:no-planners"
            );
        }

        var existing = context.ExistingPlants ?? Array.Empty<Plant>();
        var existingSnapshot = BuildExistingSnapshot(existing);

        var usedSlugs = new HashSet<string>(StringComparer.Ordinal);
        var desired = new List<DesiredPlant>();
        var summaries = new List<string>(planners.Count);
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["forum"] = "ai",
            ["plannerCount"] = planners.Count.ToString(
                System.Globalization.CultureInfo.InvariantCulture
            ),
        };

        for (var i = 0; i < planners.Count; i++)
        {
            var plannerId = (planners[i] ?? string.Empty).Trim();
            if (plannerId.Length == 0)
            {
                continue;
            }

            try
            {
                var request = new AgentChatRequest(
                    AgentId: plannerId,
                    SystemPrompt: BuildSystemPrompt(plannerId),
                    UserPrompt: BuildUserPrompt(
                        planId,
                        plannerId,
                        context.Repository,
                        existingSnapshot
                    ),
                    Temperature: 0,
                    Model: null,
                    Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["planId"] = planId,
                        ["plannerId"] = plannerId,
                        ["forum"] = "ai",
                    }
                );

                var response = await _chat.ChatAsync(request, cancellationToken);
                var parsed = TryParseResponse(
                    response,
                    out var plants,
                    out var summary,
                    out var error
                );

                if (!string.IsNullOrWhiteSpace(summary))
                {
                    summaries.Add($"{plannerId}:{summary}".Trim());
                }

                if (!parsed)
                {
                    metadata[$"planner.{plannerId}.status"] = "invalid";
                    metadata[$"planner.{plannerId}.error"] = error ?? "invalid_response";
                    continue;
                }

                metadata[$"planner.{plannerId}.status"] = "ok";

                var normalized = NormalizeProposedPlants(planId, plannerId, plants);
                normalized.Sort(static (a, b) => string.CompareOrdinal(a.Slug, b.Slug));

                foreach (var p in normalized)
                {
                    if (usedSlugs.Contains(p.Slug))
                    {
                        continue;
                    }

                    usedSlugs.Add(p.Slug);
                    desired.Add(
                        new DesiredPlant(
                            Key: $"{planId}:{p.Slug}",
                            Slug: p.Slug,
                            Title: p.Title,
                            Description: p.Description,
                            PlannerId: plannerId,
                            AssignedPlanters: p.AssignedPlanters
                        )
                    );
                }
            }
            catch (Exception ex)
            {
                metadata[$"planner.{plannerId}.status"] = "error";
                metadata[$"planner.{plannerId}.error"] = ex.GetType().Name;
            }
        }

        var finalSummary = summaries.Count == 0 ? "ai:ok" : string.Join(" | ", summaries);
        return new ReconciliationStrategy(desired, Summary: finalSummary, Metadata: metadata);
    }

    private static string BuildSystemPrompt(string plannerId)
    {
        return "You are a git-forest planner agent.\n"
            + "Your job is to propose desired plants for a plan reconcile.\n"
            + $"You are plannerId='{plannerId}'.\n"
            + "Return JSON only (no Markdown), matching the requested schema.\n"
            + "Be deterministic: do not use randomness.";
    }

    private static string BuildUserPrompt(
        string planId,
        string plannerId,
        string? repository,
        string existingPlants
    )
    {
        var repo = string.IsNullOrWhiteSpace(repository) ? "" : repository.Trim();
        return "Reconcile plan into desired plants.\n"
            + $"planId: {planId}\n"
            + (repo.Length == 0 ? "" : $"repository: {repo}\n")
            + $"plannerId: {plannerId}\n"
            + "\n"
            + "Existing plants snapshot (for context; do not duplicate slugs if avoidable):\n"
            + existingPlants
            + "\n"
            + "Output JSON object with this schema:\n"
            + "{\n"
            + "  \"desiredPlants\": [\n"
            + "    {\n"
            + "      \"slug\": \"kebab-case-slug\",\n"
            + "      \"title\": \"Short title\",\n"
            + "      \"description\": \"Optional longer description\",\n"
            + "      \"assignedPlanters\": [\"optional-planter-id\"]\n"
            + "    }\n"
            + "  ],\n"
            + "  \"summary\": \"optional short summary\"\n"
            + "}\n"
            + "\n"
            + "Rules:\n"
            + "- Provide 0..10 desiredPlants.\n"
            + "- Each slug must be unique within the response.\n"
            + "- Use deterministic slugs; avoid timestamps, hashes, or random suffixes.\n";
    }

    private static string BuildExistingSnapshot(IReadOnlyList<Plant> existingPlants)
    {
        if (existingPlants.Count == 0)
        {
            return "(none)\n";
        }

        var lines = new List<string>(existingPlants.Count);
        foreach (
            var p in existingPlants.OrderBy(x => x.Key ?? string.Empty, StringComparer.Ordinal)
        )
        {
            var key = (p.Key ?? string.Empty).Trim();
            var status = (p.Status ?? string.Empty).Trim();
            var title = (p.Title ?? string.Empty).Trim();
            if (key.Length == 0)
                continue;
            lines.Add($"- {key} [{status}] {title}".TrimEnd());
        }

        return lines.Count == 0 ? "(none)\n" : string.Join("\n", lines) + "\n";
    }

    private static bool TryParseResponse(
        AgentChatResponse response,
        out List<ProposedPlant> plants,
        out string? summary,
        out string? error
    )
    {
        plants = new List<ProposedPlant>();
        summary = null;
        error = null;

        var raw = response?.Json ?? response?.RawContent ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "empty_response";
            return false;
        }

        var json = ExtractJson(raw);
        if (string.IsNullOrWhiteSpace(json))
        {
            error = "no_json_found";
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                error = "root_not_object";
                return false;
            }

            if (root.TryGetProperty("summary", out var s) && s.ValueKind == JsonValueKind.String)
            {
                summary = s.GetString();
            }

            if (
                !root.TryGetProperty("desiredPlants", out var dp)
                || dp.ValueKind != JsonValueKind.Array
            )
            {
                // allow alternate key for convenience
                if (!root.TryGetProperty("plants", out dp) || dp.ValueKind != JsonValueKind.Array)
                {
                    error = "missing_desiredPlants";
                    return false;
                }
            }

            foreach (var item in dp.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;
                var slug =
                    item.TryGetProperty("slug", out var slugEl)
                    && slugEl.ValueKind == JsonValueKind.String
                        ? slugEl.GetString()
                        : null;
                var title =
                    item.TryGetProperty("title", out var titleEl)
                    && titleEl.ValueKind == JsonValueKind.String
                        ? titleEl.GetString()
                        : null;
                var description =
                    item.TryGetProperty("description", out var descEl)
                    && descEl.ValueKind == JsonValueKind.String
                        ? descEl.GetString()
                        : null;

                List<string>? assigned = null;
                if (
                    item.TryGetProperty("assignedPlanters", out var ap)
                    && ap.ValueKind == JsonValueKind.Array
                )
                {
                    assigned = new List<string>();
                    foreach (var p in ap.EnumerateArray())
                    {
                        if (p.ValueKind == JsonValueKind.String)
                        {
                            var v = (p.GetString() ?? string.Empty).Trim();
                            if (v.Length > 0)
                                assigned.Add(v);
                        }
                    }
                }

                plants.Add(new ProposedPlant(slug, title, description, assigned));
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.GetType().Name;
            return false;
        }
    }

    private static string ExtractJson(string raw)
    {
        var text = raw.Trim();

        // Strip ```json fences if present.
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = text.IndexOf('\n');
            if (firstNewline >= 0)
            {
                text = text[(firstNewline + 1)..];
            }

            var endFence = text.LastIndexOf("```", StringComparison.Ordinal);
            if (endFence >= 0)
            {
                text = text[..endFence];
            }

            text = text.Trim();
        }

        // If the text is already JSON, keep it; otherwise attempt to slice from first '{' to last '}'.
        if (
            text.StartsWith("{", StringComparison.Ordinal)
            && text.EndsWith("}", StringComparison.Ordinal)
        )
        {
            return text;
        }

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return text.Substring(start, end - start + 1).Trim();
        }

        return string.Empty;
    }

    private static List<NormalizedPlant> NormalizeProposedPlants(
        string planId,
        string plannerId,
        List<ProposedPlant> proposed
    )
    {
        var results = new List<NormalizedPlant>(proposed.Count);
        var used = new HashSet<string>(StringComparer.Ordinal);

        foreach (var p in proposed)
        {
            var rawSlug = (p.Slug ?? string.Empty).Trim();
            if (rawSlug.Length == 0)
            {
                // Fall back to title-derived slug.
                rawSlug = (p.Title ?? string.Empty).Trim();
            }

            var slug = NormalizeSlug(rawSlug);
            if (slug.Length == 0)
                continue;
            if (used.Contains(slug))
                continue;
            used.Add(slug);

            var title = (p.Title ?? string.Empty).Trim();
            if (title.Length == 0)
                title = slug;

            var description = (p.Description ?? string.Empty).Trim();
            var assigned = (p.AssignedPlanters ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            // Do not validate planters here; reconciliation is plants-only and planters are advisory.
            results.Add(new NormalizedPlant(slug, title, description, assigned));
        }

        _ = planId;
        _ = plannerId;
        return results;
    }

    private static string NormalizeSlug(string input)
    {
        var trimmed = (input ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(trimmed.Length);
        var lastWasDash = false;
        foreach (var ch in trimmed)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
                lastWasDash = false;
                continue;
            }

            if (ch is '-' or '_' or ' ' or '.')
            {
                if (!lastWasDash)
                {
                    sb.Append('-');
                    lastWasDash = true;
                }
            }
        }

        return sb.ToString().Trim('-');
    }

    private sealed record ProposedPlant(
        string? Slug,
        string? Title,
        string? Description,
        List<string>? AssignedPlanters
    );

    private sealed record NormalizedPlant(
        string Slug,
        string Title,
        string Description,
        IReadOnlyList<string> AssignedPlanters
    );
}
