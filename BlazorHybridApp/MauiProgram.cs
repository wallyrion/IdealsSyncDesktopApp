using BlazorHybridApp.Core;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using H.NotifyIcon;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using MudBlazor.Services;
using YourNamespace.Platforms.Windows;

namespace BlazorHybridApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder

                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseNotifyIcon()
                .ConfigureLifecycleEvents(e =>
                {
#if WINDOWS
                    e.ConfigureMinimizeToTray();
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddDbContext<AppDbContext>(o =>
            {
                o.UseSqlite($"Filename=app6.db");
            });

            builder.Services.AddSingleton<FolderSelector>();
            builder.Services.AddSingleton<IFolderPicker>(FolderPicker.Default);
            builder.Services.AddSingleton<State>();
            builder.Services.AddSingleton<ExplorerService>();
            
            builder.Services.AddScoped(sp =>
                new HttpClient
                {
                    BaseAddress = new Uri("http://localhost:5238/")
                });

            builder.Services.AddScoped<FileSyncHttpClient>();
            builder.Services.AddScoped<FileSyncService>();
            builder.Services.AddSingleton<BackgroundTest>();
            
/*#if WINDOWS
            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddWindows(windowsLifecycleBuilder =>
                {
                    windowsLifecycleBuilder.OnWindowCreated(window =>
                    {
                        //we need this to use Microsoft.UI.Windowing functions for our window
                        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);

                        //and here it is
                        appWindow.Closing += async (s, e) => 
                        {
                            e.Cancel = true;
                            bool result = await App.Current.MainPage.DisplayAlert(
                                "Alert title", 
                                "You sure want to close app?", 
                                "Yes", 
                                "Cancel");

                            if (result)
                            {
                                App.Current.Quit();
                            }
                        };
                    });
                });
            });
           
#endif*/
            
           

            builder.Services.AddMudServices();
            

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            var build = builder.Build();
            
            using (var scope = build.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.EnsureCreated();

                
            }

            return build;
        }
    }
}
