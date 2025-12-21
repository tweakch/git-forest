using System.Diagnostics.CodeAnalysis;

namespace GitForest.Core.Services;

public interface IGitService
{
    string GetRepoRoot(string? workingDirectory = null);

    bool BranchExists(string branchName, string? workingDirectory = null);

    void CreateBranch(string branchName, string? workingDirectory = null);

    void CheckoutBranch(string branchName, bool createIfMissing, string? workingDirectory = null);

    bool HasUncommittedChanges(string? workingDirectory = null);

    void AddAll(string? workingDirectory = null);

    void Commit(string message, string? workingDirectory = null);
}

public sealed class GitServiceException : Exception
{
    public string[] Arguments { get; }
    public int ExitCode { get; }
    public string StdOut { get; }
    public string StdErr { get; }

    public GitServiceException(
        IReadOnlyList<string> arguments,
        int exitCode,
        string stdOut,
        string stdErr
    )
        : base($"git {string.Join(' ', arguments)} failed with exit code {exitCode}")
    {
        Arguments = arguments is null ? Array.Empty<string>() : arguments.ToArray();
        ExitCode = exitCode;
        StdOut = stdOut ?? string.Empty;
        StdErr = stdErr ?? string.Empty;
    }
}

public interface IPlanterDiscovery
{
    IReadOnlyList<string> ListCustomPlanterIds();

    bool CustomPlanterExists(string planterId);
}

public interface IPlanterGrowthApplier
{
    void ApplyDeterministicGrowth(string repoRoot, string plantKey, string planterId);
}
