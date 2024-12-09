#if WINDOWS
using IdealsSyncDesktopApp.Core;
using Microsoft.Maui.LifecycleEvents;
using Application = Microsoft.Maui.Controls.Application;

namespace IdealsSyncDesktopApp;

public static class MinimizeToTrayHelper
{
    public static void ConfigureMinimizeToTray(this ILifecycleBuilder events)
    {
            
        events.AddWindows(lifecycleBuilder => lifecycleBuilder.OnWindowCreated(window =>
        {
            window.ExtendsContentIntoTitleBar = true;
            var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);

                
            appWindow.Closing += async (s, e) =>
            {
                e.Cancel = true;
                var appShouldBeClosed = await Application.Current?.MainPage?.DisplayAlert(
                    "App close",
                    "Do you really want to quit?",
                    "Close",
                    "Minimize to system tray")!;

                if (appShouldBeClosed)
                {
                    Application.Current?.Quit();
                }

                else
                {
                    GlobalEvents.CloseApp();
                }
            };
        }));
    }

       
}
#endif
