using System.Diagnostics;

namespace IdealsSyncDesktopApp.Core;

public static class LocationHelper
{
    public static string GetDbPath()
    {
        string exePath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName)!;
        string dbPath = Path.Combine(exePath, "app_database.db");

        return dbPath;
    }
}