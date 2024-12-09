using System.Diagnostics;

namespace IdealsSyncDesktopApp.Core;

public static class RegistryHelper
{
    private const string AppName = "IdealsSyncDesktopApp";
    
    public static void RegisterOpenWithIdeals()
    {
        string exePath = LocationHelper.GetExecutableAbsolutePath();
        var icoPath = LocationHelper.GetIcoAbsolutePath();
        
        
        var regCommands = new[]
        {
            // Folder-specific commands
            $"""
              reg add "HKCU\Software\Classes\Directory\shell\{AppName}" /v "" /t REG_SZ /d "Make as Ideals synchronization folder" /f
              """,
            $"""
              reg add "HKCU\Software\Classes\Directory\shell\{AppName}\command" /v "" /t REG_SZ /d "\"{exePath}\" \"%1\" /folder" /f
              """,
            $"""
              reg add "HKCU\Software\Classes\Directory\shell\{AppName}" /v "Icon" /t REG_SZ /d "{icoPath}" /f
              """,

            // File-specific commands
            $"""
              reg add "HKCU\Software\Classes\*\shell\{AppName}" /v "" /t REG_SZ /d "Copy to ideals folder" /f
              """,
            $"""
              reg add "HKCU\Software\Classes\*\shell\{AppName}\command" /v "" /t REG_SZ /d "\"{exePath}\" \"%1\" /file" /f
              """,
            $"""
              reg add "HKCU\Software\Classes\*\shell\{AppName}" /v "Icon" /t REG_SZ /d "{icoPath}" /f
              """
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