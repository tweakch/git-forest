using GitForest.Application.Features.Plants.Commands;
using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Core.Specifications.Plants;

namespace GitForest.Cli.Features;

internal static class PlantSelector
{
    public static async Task<Plant> ResolveAsync(
        IPlantRepository plants,
        string selector,
        CancellationToken cancellationToken
    )
    {
        if (plants is null)
            throw new ArgumentNullException(nameof(plants));

        var sel = (selector ?? string.Empty).Trim();
        if (sel.Length == 0)
        {
            throw new PlantNotFoundException(selector ?? string.Empty);
        }

        var all = await plants.ListAsync(new AllPlantsSpec(), cancellationToken);
        var matches = FindMatches(all, sel);
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

    public static IReadOnlyList<Plant> FindMatches(IReadOnlyList<Plant> plants, string selector)
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
        // Accept P01, p1, P0003 – 1-based ordinal; convert to 0-based index.
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
}
