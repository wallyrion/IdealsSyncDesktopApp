using System.Diagnostics;

namespace IdealsSyncDesktopApp.Core;

public static class LocationHelper
{
    public static string GetExecutableDirectoryAbsolutePath()
    {
        string exePath = Process.GetCurrentProcess().MainModule.FileName;

        var directory = Path.GetDirectoryName(exePath);

        return directory;

    }

    public static string GetDbPath()
    {
        var directory = GetExecutableDirectoryAbsolutePath();

        /*string exePath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName)!;
        string dbPath = Path.Combine(exePath, "app_database.db");*/

        return Path.Combine(directory, "app_database.db");
    }

    public static string GetDbConnectionString()
    {
        var dbPath = GetDbPath();

        return $"Data Source={dbPath}";
    }
}