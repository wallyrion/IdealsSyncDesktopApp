﻿#if WINDOWS
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using System.Windows.Forms;
using System.Drawing;
using BlazorHybridApp.Core;
using H.NotifyIcon;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI;
using MudBlazor;
using Application = Microsoft.Maui.Controls.Application;
using Window = ABI.Microsoft.UI.Xaml.Window;

namespace YourNamespace.Platforms.Windows
{
    public static class MinimizeToTrayHelper
    {
        public static void ConfigureMinimizeToTray(this ILifecycleBuilder events)
        {
            /*events.AddWindows(windows => windows
                .OnClosed((window, _) => RaiseWindowClosedEvent(window)));
                */
            
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
                        AppGlobalState.CloseApp();
                    }
                };
            }));
        }

       
    }
}
#endif