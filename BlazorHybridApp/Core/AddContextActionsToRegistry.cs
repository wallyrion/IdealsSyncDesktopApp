using System.Diagnostics;

namespace BlazorHybridApp.Core;

public static class RegistryHelper
{
    public static void RegisterOpenWithIdeals()
    {
        string exePath = Process.GetCurrentProcess().MainModule.FileName;
        string icoPath = Path.Combine(Path.GetDirectoryName(exePath), "ideals.ico");

        var regCommands = new[]
        {
            // Directory
            $""""
             reg add "HKCU\Software\Classes\Directory\shell\YourApp" /v "" /t REG_SZ /d "Sync with ideals" /f 

             """",
            $""""
             reg add "HKCU\Software\Classes\Directory\shell\YourApp\command" /v "" /t REG_SZ /d "\"{exePath}\" \"%1\"" /f ,
             """",

            $""""
             reg add "HKCU\Software\Classes\Directory\shell\YourApp" /v "Icon" /t REG_SZ /d "{icoPath}" /f 

             """"
        };

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