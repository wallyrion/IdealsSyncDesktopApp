using System.Diagnostics;

namespace IdealsSyncDesktopApp.Core;

public static class RegistryHelper
{
    public static void RegisterOpenWithIdeals()
    {
        string exePath = Process.GetCurrentProcess().MainModule.FileName;
        string icoPath = Path.Combine(Path.GetDirectoryName(exePath), "ideals.ico");

        var regCommands = new[]
        {
            // Folder-specific commands
            $"reg add \"HKCU\\Software\\Classes\\Directory\\shell\\YourAppFolder\" /v \"\" /t REG_SZ /d \"Make as Ideals synchronization folder\" /f",
            $"reg add \"HKCU\\Software\\Classes\\Directory\\shell\\YourAppFolder\\command\" /v \"\" /t REG_SZ /d \"\\\"{exePath}\\\" \\\"%1\\\" /folder\" /f",
            $"reg add \"HKCU\\Software\\Classes\\Directory\\shell\\YourAppFolder\" /v \"Icon\" /t REG_SZ /d \"{icoPath}\" /f",

            // File-specific commands
            $"reg add \"HKCU\\Software\\Classes\\*\\shell\\YourAppFile\" /v \"\" /t REG_SZ /d \"Copy to ideals folder\" /f",
            $"reg add \"HKCU\\Software\\Classes\\*\\shell\\YourAppFile\\command\" /v \"\" /t REG_SZ /d \"\\\"{exePath}\\\" \\\"%1\\\" /file\" /f",
            $"reg add \"HKCU\\Software\\Classes\\*\\shell\\YourAppFile\" /v \"Icon\" /t REG_SZ /d \"{icoPath}\" /f"
        };



            // Directory
            /*
            $"""""
             reg add "HKCU\Software\Classes\Directory\shell\YourApp3" /v "" /t REG_SZ /d "Sync with ideals3" /f 

             """",
            $""""
             reg add "HKCU\Software\Classes\Directory\shell\YourApp3\command" /v "" /t REG_SZ /d "\"{exePath}\" \"%1\"" /f ,
             """",
             */
           

        foreach (var cmd in regCommands)
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {cmd}",
                CreateNoWindow = true,
                UseShellExecute = false
            });

            process.WaitForExit();
        }
    }
}