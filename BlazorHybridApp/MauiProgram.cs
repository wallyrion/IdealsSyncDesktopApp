using System;
using System.Net.Http;
using System.Threading.Tasks;
using BlazorHybridApp.Components;
using BlazorHybridApp.Core;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using static BlazorHybridApp.Components.Pages.Home;

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
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddDbContext<AppDbContext>(o =>
            {
                o.UseSqlite($"Filename=app1.db");
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


#if WINDOWS
builder.Services.AddSingleton<IFolderPickerService, WindowsFolderPickerService>();
#endif


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
