namespace GitForest.Application.Features.Planters;

internal static class PlanterBranchNaming
{
    public static string ComputeBranchName(string planterId, string plantKey, string? branchOption)
    {
        var pid = (planterId ?? string.Empty).Trim();
        var key = (plantKey ?? string.Empty).Trim();
        if (pid.Length == 0 || key.Length == 0)
        {
            return "git-forest/untitled";
        }

        var opt = (branchOption ?? "auto").Trim();
        if (!string.Equals(opt, "auto", StringComparison.OrdinalIgnoreCase) && opt.Length > 0)
        {
            return NormalizeBranchName(opt);
        }

        var (planId, slug) = SplitPlantKey(key);
        return NormalizeBranchName($"{pid}/{planId}__{slug}");
    }

    private static (string planId, string slug) SplitPlantKey(string plantKey)
    {
        var key = (plantKey ?? string.Empty).Trim();
        if (key.Length == 0)
        {
            return (string.Empty, string.Empty);
        }

        var idx = key.IndexOf(':', StringComparison.Ordinal);
        if (idx <= 0 || idx == key.Length - 1)
        {
            return (key, string.Empty);
        }

        return (key[..idx], key[(idx + 1)..]);
    }

    private static string NormalizeBranchName(string input)
    {
        var trimmed = (input ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return "git-forest/untitled";
        }

        var sb = new System.Text.StringBuilder(trimmed.Length);
        var lastWasDash = false;
        foreach (var ch in trimmed)
        {
            if (char.IsLetterOrDigit(ch) || ch is '/' or '-' or '_' or '.')
            {
                sb.Append(ch);
                lastWasDash = false;
                continue;
            }

            if (!lastWasDash)
            {
                sb.Append('-');
                lastWasDash = true;
            }
        }

        var normalized = sb.ToString().Trim('-');
        return normalized.Length == 0 ? "git-forest/untitled" : normalized;
    }
}
