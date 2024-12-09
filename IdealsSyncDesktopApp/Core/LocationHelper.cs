using System.Diagnostics;

namespace BlazorHybridApp.Core;

public static class LocationHelper
{
    public static string GetDbPath()
    {
        string exePath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName)!;
        string dbPath = Path.Combine(exePath, "app_database.db");

        return dbPath;
        /*string dbFileName = "app_database.db";

        string dbPath = Path.Combine(FileSystem.AppDataDirectory, dbFileName);*/
    }
}