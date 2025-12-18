namespace GitForest.Cli;

/// <summary>
/// Stable exit codes for automation (keep aligned with README.md / CLI.md).
/// </summary>
public static class ExitCodes
{
    public const int Success = 0;
    public const int InvalidArguments = 2;

    public const int ForestNotInitialized = 10;
    public const int PlanNotFound = 11;
    public const int PlantNotFoundOrAmbiguous = 12;
    public const int PlanterNotFound = 13;

    public const int SchemaValidationFailed = 20;
    public const int LockTimeoutOrBusy = 23;
    public const int OrleansNotAvailable = 24;
    public const int GitOperationFailed = 30;
    public const int ExecutionNotPermitted = 40;
}
