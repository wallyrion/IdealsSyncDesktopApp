using System.Diagnostics;
using BlazorHybridApp.Core;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using H.NotifyIcon;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using MudBlazor.Services;
using YourNamespace.Platforms.Windows;

namespace IdealsSyncDesktopApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder.Configuration.AddJsonFile("appsettings.json");
            
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
                var dbPath = LocationHelper.GetDbPath();

                o.UseSqlite($"Data Source={dbPath}");
            });

            builder.Services.AddSingleton<UserSettingsProvider>();
            builder.Services.AddSingleton<IFolderPicker>(FolderPicker.Default);
            builder.Services.AddSingleton<State>();
            builder.Services.AddSingleton<ExplorerService>();

            var useSharedServer = builder.Configuration["UseSharedServer"];
            var apiBaseUrl = useSharedServer.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) 
                ? builder.Configuration["ApiServer"] : builder.Configuration["LocalApi"];
            builder.Services.AddScoped(sp =>
                new HttpClient
                {
                    BaseAddress = new Uri(apiBaseUrl!)
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
