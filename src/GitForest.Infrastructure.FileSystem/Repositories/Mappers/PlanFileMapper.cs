using System.Globalization;
using System.Text.Json;
using GitForest.Core;
using GitForest.Infrastructure.FileSystem.Serialization;

namespace GitForest.Infrastructure.FileSystem.Repositories;

internal static class PlanFileMapper
{
    public static Plan ToDomain(
        PlanYamlLite.ParsedPlan parsed,
        string fallbackId,
        string source,
        DateTime installedDateUtc)
    {
        return new Plan
        {
            Id = string.IsNullOrWhiteSpace(parsed.Id) ? fallbackId : parsed.Id,
            Version = parsed.Version ?? string.Empty,
            Source = source,
            Author = parsed.Author ?? string.Empty,
            License = parsed.License ?? string.Empty,
            Repository = parsed.Repository ?? string.Empty,
            Homepage = parsed.Homepage ?? string.Empty,
            Planners = (parsed.Planners ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
            Planters = (parsed.Planters ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
            InstalledDate = installedDateUtc
        };
    }

    public static string SerializeMinimalYaml(string planId, Plan plan)
    {
        if (plan is null) throw new ArgumentNullException(nameof(plan));

        return PlanYamlLite.SerializeMinimal(
            id: planId,
            version: plan.Version ?? string.Empty,
            author: plan.Author ?? string.Empty,
            license: plan.License ?? string.Empty,
            repository: plan.Repository ?? string.Empty,
            homepage: plan.Homepage ?? string.Empty,
            planners: plan.Planners ?? new List<string>(),
            planters: plan.Planters ?? new List<string>());
    }

    public static string SerializeInstallJsonForAdd(string planId, Plan plan)
    {
        if (plan is null) throw new ArgumentNullException(nameof(plan));

        // Best-effort install metadata (source + installedAt) for compatibility with existing tooling.
        var installedAt = plan.InstalledDate == default
            ? DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            : new DateTimeOffset(DateTime.SpecifyKind(plan.InstalledDate, DateTimeKind.Utc)).ToString("O", CultureInfo.InvariantCulture);

        var metadata = new { id = planId, source = plan.Source ?? string.Empty, installedAt };
        return JsonSerializer.Serialize(metadata, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    public static DateTime? TryReadInstalledAt(string installJsonPath)
    {
        if (!File.Exists(installJsonPath))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(FileSystemRepositoryFs.ReadAllTextUtf8(installJsonPath));
            if (doc.RootElement.TryGetProperty("installedAt", out var iat))
            {
                var text = iat.GetString();
                if (!string.IsNullOrWhiteSpace(text) &&
                    DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
                {
                    return dto.UtcDateTime;
                }
            }
        }
        catch
        {
            // best-effort metadata
        }

        return null;
    }

    public static string TryReadPlanSource(string installJsonPath)
    {
        if (!File.Exists(installJsonPath))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(FileSystemRepositoryFs.ReadAllTextUtf8(installJsonPath));
            if (doc.RootElement.TryGetProperty("source", out var src))
            {
                return src.GetString() ?? string.Empty;
            }
        }
        catch
        {
            // best-effort metadata
        }

        return string.Empty;
    }
}


