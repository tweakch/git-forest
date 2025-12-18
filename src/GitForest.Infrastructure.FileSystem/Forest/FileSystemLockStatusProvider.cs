using GitForest.Core.Services;

namespace GitForest.Infrastructure.FileSystem.Forest;

public sealed class FileSystemLockStatusProvider : ILockStatusProvider
{
    private readonly string _forestDir;

    public FileSystemLockStatusProvider(string forestDir)
    {
        _forestDir = forestDir ?? string.Empty;
    }

    public string GetLockStatus()
    {
        var lockPath = Path.Combine(_forestDir.Trim(), "lock");
        var lockStatus = "free";
        try
        {
            if (File.Exists(lockPath))
            {
                var lockText = File.ReadAllText(lockPath).Trim();
                if (!string.IsNullOrWhiteSpace(lockText))
                {
                    lockStatus = "held";
                }
            }
        }
        catch
        {
            // best-effort lock status
        }

        return lockStatus;
    }
}
