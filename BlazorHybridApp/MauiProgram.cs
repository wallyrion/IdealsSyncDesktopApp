using BlazorHybridApp.Core;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using H.NotifyIcon;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.Storage;
using MudBlazor.Services;
using System.Diagnostics;
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
                var dbPath = Process.GetCurrentProcess().MainModule!.FileName.Replace("BlazorHybridApp.exe", "fileRequests.db");

                o.UseSqlite($"Data Source={dbPath}");
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
            
            builder.Services.AddMudServices();
            builder.Services.AddSingleton<GlobalEvents>();

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

                var globalevents = scope.ServiceProvider.GetRequiredService<GlobalEvents>();
                globalevents.Initialize();


            }

            return build;
        }
    }
}
