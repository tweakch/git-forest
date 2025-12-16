namespace GitForest.Core.Services;

/// <summary>
/// Infrastructure port for creating/initializing the on-disk forest structure.
/// </summary>
public interface IForestInitializer
{
    void Initialize(string forestDir);
}

/// <summary>
/// Infrastructure port for retrieving the forest lock status.
/// </summary>
public interface ILockStatusProvider
{
    string GetLockStatus();
}

/// <summary>
/// Infrastructure port for installing a plan package into the forest.
/// </summary>
public interface IPlanInstaller
{
    Task<(string planId, string version)> InstallAsync(string source, CancellationToken cancellationToken = default);
}

/// <summary>
/// Infrastructure port for reconciling a plan into plants on disk.
/// </summary>
public interface IPlanReconciler
{
    Task<(string planId, int plantsCreated, int plantsUpdated)> ReconcileAsync(string planId, bool dryRun, CancellationToken cancellationToken = default);
}

