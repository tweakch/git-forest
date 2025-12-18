using System.Text;

namespace GitForest.Infrastructure.FileSystem.Repositories;

internal static class FileSystemRepositoryFs
{
    private static readonly Encoding Utf8 = Encoding.UTF8;

    public static string ReadAllTextUtf8(string path)
    {
        return File.ReadAllText(path, Utf8);
    }

    public static void WriteAllTextUtf8(string path, string contents)
    {
        File.WriteAllText(path, contents, Utf8);
    }

    public static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    /// <summary>
    /// Scans <paramref name="parentDir"/> for subdirectories that contain <paramref name="yamlFileName"/>,
    /// loads the YAML file as UTF-8 text, and converts it via <paramref name="loader"/>.
    /// </summary>
    public static List<T> LoadAllFromSubdirectories<T>(
        string parentDir,
        string yamlFileName,
        Func<string, string, string, T> loader
    )
    {
        if (loader is null)
            throw new ArgumentNullException(nameof(loader));

        if (!Directory.Exists(parentDir))
        {
            return new List<T>();
        }

        var results = new List<T>();
        foreach (var entityDir in Directory.GetDirectories(parentDir))
        {
            var yamlPath = Path.Combine(entityDir, yamlFileName);
            if (!File.Exists(yamlPath))
            {
                continue;
            }

            var entityId = Path.GetFileName(entityDir);
            var yaml = ReadAllTextUtf8(yamlPath);
            results.Add(loader(entityDir, entityId, yaml));
        }

        return results;
    }
}
