using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Core.Specifications.Plants;
using GitForest.Mediator;

namespace GitForest.Application.Features.Plants.Commands;

public sealed record AssignPlanterToPlantCommand(string Selector, string PlanterId, bool DryRun)
    : IRequest<Plant>;

public sealed class PlantNotFoundException : Exception
{
    public string Selector { get; }

    public PlantNotFoundException(string selector)
        : base($"Plant not found: '{selector}'.")
    {
        Selector = selector;
    }
}

public sealed class PlantAmbiguousSelectorException : Exception
{
    public string Selector { get; }
    public string[] Matches { get; }

    public PlantAmbiguousSelectorException(string selector, string[] matches)
        : base($"Plant selector is ambiguous: '{selector}'.")
    {
        Selector = selector;
        Matches = matches ?? Array.Empty<string>();
    }
}

internal sealed class AssignPlanterToPlantHandler
    : IRequestHandler<AssignPlanterToPlantCommand, Plant>
{
    private readonly IPlantRepository _plants;

    public AssignPlanterToPlantHandler(IPlantRepository plants)
    {
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
    }

    public async Task<Plant> Handle(
        AssignPlanterToPlantCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var resolved = await ResolvePlantAsync(request.Selector, cancellationToken);
        var updated = Clone(resolved);

        var normalizedPlanterId = (request.PlanterId ?? string.Empty).Trim();
        if (normalizedPlanterId.Length == 0)
        {
            return resolved;
        }

        var planters = (updated.AssignedPlanters ?? new List<string>()).ToList();
        if (
            !planters.Any(p =>
                string.Equals(p, normalizedPlanterId, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            planters.Add(normalizedPlanterId);
        }

        updated.AssignedPlanters = planters;

        if (string.Equals(updated.Status, "planned", StringComparison.OrdinalIgnoreCase))
        {
            updated.Status = "planted";
        }

        if (!request.DryRun)
        {
            updated.LastActivityDate = DateTime.UtcNow;
            await _plants.UpdateAsync(updated, cancellationToken);
        }

        return updated;
    }

    private async Task<Plant> ResolvePlantAsync(
        string selector,
        CancellationToken cancellationToken
    )
    {
        var sel = (selector ?? string.Empty).Trim();
        if (sel.Length == 0)
        {
            throw new PlantNotFoundException(selector ?? string.Empty);
        }

        var plants = await _plants.ListAsync(new AllPlantsSpec(), cancellationToken);
        var matches = FindMatches(plants, sel);
        if (matches.Count == 1)
        {
            return matches[0];
        }

        if (matches.Count == 0)
        {
            throw new PlantNotFoundException(selector ?? string.Empty);
        }

        throw new PlantAmbiguousSelectorException(
            selector ?? string.Empty,
            matches.Select(p => p.Key).ToArray()
        );
    }

    internal static IReadOnlyList<Plant> FindMatches(IReadOnlyList<Plant> plants, string selector)
    {
        var sel = (selector ?? string.Empty).Trim();
        if (sel.Length == 0 || plants.Count == 0)
        {
            return Array.Empty<Plant>();
        }

        // 1) Exact key match: <plan-id>:<slug>
        var exact = plants
            .Where(p => string.Equals(p.Key, sel, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (exact.Length > 0)
        {
            return exact;
        }

        // 2) Pxx style stable index into ordered list (best-effort; deterministic ordering).
        if (TryParsePIndex(sel, out var index))
        {
            var ordered = plants.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase).ToArray();
            if (index >= 0 && index < ordered.Length)
            {
                return new[] { ordered[index] };
            }

            return Array.Empty<Plant>();
        }

        // 3) Slug match: match any plant whose key right-side equals selector.
        var slugMatches = plants
            .Where(p =>
            {
                var key = p.Key ?? string.Empty;
                var idx = key.IndexOf(':', StringComparison.Ordinal);
                if (idx < 0 || idx == key.Length - 1)
                {
                    return false;
                }

                var slug = key[(idx + 1)..];
                return string.Equals(slug, sel, StringComparison.OrdinalIgnoreCase);
            })
            .ToArray();

        return slugMatches;
    }

    private static bool TryParsePIndex(string selector, out int index)
    {
        // Accept P01, p1, P0003 → 1-based ordinal; convert to 0-based index.
        index = -1;
        if (selector.Length < 2)
        {
            return false;
        }

        if (selector[0] != 'p' && selector[0] != 'P')
        {
            return false;
        }

        var digits = selector[1..].Trim();
        if (digits.Length == 0)
        {
            return false;
        }

        foreach (var ch in digits)
        {
            if (!char.IsDigit(ch))
            {
                return false;
            }
        }

        if (!int.TryParse(digits, out var oneBased) || oneBased <= 0)
        {
            return false;
        }

        index = oneBased - 1;
        return true;
    }

    internal static Plant Clone(Plant source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        return new Plant
        {
            Key = source.Key,
            Slug = source.Slug,
            PlanId = source.PlanId,
            PlannerId = source.PlannerId,
            Status = source.Status,
            Title = source.Title,
            Description = source.Description,
            AssignedPlanters = source.AssignedPlanters is null
                ? new List<string>()
                : new List<string>(source.AssignedPlanters),
            Branches = source.Branches is null
                ? new List<string>()
                : new List<string>(source.Branches),
            CreatedDate = source.CreatedDate,
            LastActivityDate = source.LastActivityDate,
        };
    }
}

public sealed record UnassignPlanterFromPlantCommand(string Selector, string PlanterId, bool DryRun)
    : IRequest<Plant>;

internal sealed class UnassignPlanterFromPlantHandler
    : IRequestHandler<UnassignPlanterFromPlantCommand, Plant>
{
    private readonly IPlantRepository _plants;

    public UnassignPlanterFromPlantHandler(IPlantRepository plants)
    {
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
    }

    public async Task<Plant> Handle(
        UnassignPlanterFromPlantCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var resolved = await ResolvePlantAsync(request.Selector, cancellationToken);

        var updated = AssignPlanterToPlantHandler.Clone(resolved);

        var normalizedPlanterId = (request.PlanterId ?? string.Empty).Trim();
        var planters = (updated.AssignedPlanters ?? new List<string>())
            .Where(p =>
                !string.Equals(
                    (p ?? string.Empty).Trim(),
                    normalizedPlanterId,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();

        updated.AssignedPlanters = planters;

        if (!request.DryRun)
        {
            updated.LastActivityDate = DateTime.UtcNow;
            await _plants.UpdateAsync(updated, cancellationToken);
        }

        return updated;
    }

    private async Task<Plant> ResolvePlantAsync(
        string selector,
        CancellationToken cancellationToken
    )
    {
        var sel = (selector ?? string.Empty).Trim();
        if (sel.Length == 0)
        {
            throw new PlantNotFoundException(selector ?? string.Empty);
        }

        var plants = await _plants.ListAsync(new AllPlantsSpec(), cancellationToken);
        var matches = AssignPlanterToPlantHandler.FindMatches(plants, sel);
        if (matches.Count == 1)
        {
            return matches[0];
        }

        if (matches.Count == 0)
        {
            throw new PlantNotFoundException(selector ?? string.Empty);
        }

        throw new PlantAmbiguousSelectorException(
            selector ?? string.Empty,
            matches.Select(p => p.Key).ToArray()
        );
    }
}

public sealed record HarvestPlantCommand(string Selector, bool Force, bool DryRun)
    : IRequest<Plant>;

internal sealed class HarvestPlantHandler : IRequestHandler<HarvestPlantCommand, Plant>
{
    private readonly IPlantRepository _plants;

    public HarvestPlantHandler(IPlantRepository plants)
    {
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
    }

    public async Task<Plant> Handle(
        HarvestPlantCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var resolved = await ResolvePlantAsync(request.Selector, cancellationToken);
        var updated = AssignPlanterToPlantHandler.Clone(resolved);

        if (
            !request.Force
            && !string.Equals(updated.Status, "harvestable", StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new InvalidOperationException(
                $"Plant is not harvestable (status={updated.Status})."
            );
        }

        updated.Status = "harvested";

        if (!request.DryRun)
        {
            updated.LastActivityDate = DateTime.UtcNow;
            await _plants.UpdateAsync(updated, cancellationToken);
        }

        return updated;
    }

    private async Task<Plant> ResolvePlantAsync(
        string selector,
        CancellationToken cancellationToken
    )
    {
        var sel = (selector ?? string.Empty).Trim();
        if (sel.Length == 0)
        {
            throw new PlantNotFoundException(selector ?? string.Empty);
        }

        var plants = await _plants.ListAsync(new AllPlantsSpec(), cancellationToken);
        var matches = AssignPlanterToPlantHandler.FindMatches(plants, sel);
        if (matches.Count == 1)
        {
            return matches[0];
        }

        if (matches.Count == 0)
        {
            throw new PlantNotFoundException(selector ?? string.Empty);
        }

        throw new PlantAmbiguousSelectorException(
            selector ?? string.Empty,
            matches.Select(p => p.Key).ToArray()
        );
    }
}

public sealed record ArchivePlantCommand(string Selector, bool Force, bool DryRun)
    : IRequest<Plant>;

internal sealed class ArchivePlantHandler : IRequestHandler<ArchivePlantCommand, Plant>
{
    private readonly IPlantRepository _plants;

    public ArchivePlantHandler(IPlantRepository plants)
    {
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
    }

    public async Task<Plant> Handle(
        ArchivePlantCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var resolved = await ResolvePlantAsync(request.Selector, cancellationToken);
        var updated = AssignPlanterToPlantHandler.Clone(resolved);

        if (
            !request.Force
            && !string.Equals(updated.Status, "harvested", StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new InvalidOperationException(
                $"Plant is not harvested (status={updated.Status})."
            );
        }

        updated.Status = "archived";

        if (!request.DryRun)
        {
            updated.LastActivityDate = DateTime.UtcNow;
            await _plants.UpdateAsync(updated, cancellationToken);
        }

        return updated;
    }

    private async Task<Plant> ResolvePlantAsync(
        string selector,
        CancellationToken cancellationToken
    )
    {
        var sel = (selector ?? string.Empty).Trim();
        if (sel.Length == 0)
        {
            throw new PlantNotFoundException(selector ?? string.Empty);
        }

        var plants = await _plants.ListAsync(new AllPlantsSpec(), cancellationToken);
        var matches = AssignPlanterToPlantHandler.FindMatches(plants, sel);
        if (matches.Count == 1)
        {
            return matches[0];
        }

        if (matches.Count == 0)
        {
            throw new PlantNotFoundException(selector ?? string.Empty);
        }

        throw new PlantAmbiguousSelectorException(
            selector ?? string.Empty,
            matches.Select(p => p.Key).ToArray()
        );
    }
}

public sealed record RemovePlantCommand(string Selector, bool Force, bool DryRun) : IRequest<Plant>;

internal sealed class RemovePlantHandler : IRequestHandler<RemovePlantCommand, Plant>
{
    private readonly IPlantRepository _plants;

    public RemovePlantHandler(IPlantRepository plants)
    {
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
    }

    public async Task<Plant> Handle(RemovePlantCommand request, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var resolved = await ResolvePlantAsync(request.Selector, cancellationToken);

        if (
            !request.Force
            && !string.Equals(resolved.Status, "archived", StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new InvalidOperationException(
                $"Plant must be archived before removal (status={resolved.Status}). Use --force to override."
            );
        }

        var snapshot = AssignPlanterToPlantHandler.Clone(resolved);

        if (!request.DryRun)
        {
            await _plants.DeleteAsync(resolved, cancellationToken);
        }

        return snapshot;
    }

    private async Task<Plant> ResolvePlantAsync(string selector, CancellationToken cancellationToken)
    {
        var sel = (selector ?? string.Empty).Trim();
        if (sel.Length == 0)
        {
            throw new PlantNotFoundException(selector ?? string.Empty);
        }

        var plants = await _plants.ListAsync(new AllPlantsSpec(), cancellationToken);
        var matches = AssignPlanterToPlantHandler.FindMatches(plants, sel);
        if (matches.Count == 1)
        {
            return matches[0];
        }

        if (matches.Count == 0)
        {
            throw new PlantNotFoundException(selector ?? string.Empty);
        }

        throw new PlantAmbiguousSelectorException(
            selector ?? string.Empty,
            matches.Select(p => p.Key).ToArray()
        );
    }
}
