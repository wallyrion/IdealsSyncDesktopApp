using System.Diagnostics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System.IO.MemoryMappedFiles;
using System.Threading;
using BlazorHybridApp.Core;
using H.NotifyIcon.Apps.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Platform;

namespace BlazorHybridApp
{
    public partial class App : Application
    {
        private static Mutex _mutex;
        private const string MutexName = "YourAppMutexName";
        private readonly ServiceCollection Services;
        
        public App()
        {
            //InitializeComponent();

            
            InitializeComponent();
            var services = new ServiceCollection();
            var mainModule = Process.GetCurrentProcess().MainModule;
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={mainModule.FileName.Replace("BlazorHybridApp.exe","fileRequests.db")}"));
            Services = services;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            _mutex = new Mutex(true, MutexName, out bool createdNew);

            if (!createdNew)
            {
                // Another instance exists, send file path to it
                var args = Environment.GetCommandLineArgs();
                if (args.Length > 1)
                {
                    var _dbContext = Services.BuildServiceProvider().GetRequiredService<AppDbContext>();
                    _dbContext.Database.EnsureCreated();
                    
                    _dbContext.AppEvents.Add(new AppEvent
                    {
                        Id = Guid.NewGuid(),
                        Details = args[1],
                        Name = "Requested from explorer"
                    });

                    _dbContext.SaveChanges();
                
                    // Signal the existing instance
                    using (var eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "YourAppEvent"))
                    {
                        eventWaitHandle.Set();
                    }
                
                    Environment.Exit(0);
                    return null;
                }
            }

            // Start listening for file open requests in a background task
            ListenForFileOpenRequests();

            //return new Window(new MainPage()) { Title = "BlazorHybridApp" };
            
            var window = new Window(new AppShell());


            window.Width = 1400;
            window.Height = 800;
            window.MinimumWidth = 600;
            window.MinimumHeight = 600;


            return window;
        }
        
        /*protected override Window CreateWindow(IActivationState? activationState)
        {
            
        }*/

        private void ListenForFileOpenRequests()
        {
            Task.Run(() =>
            {
                using (var eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "YourAppEvent"))
                {
                    while (true)
                    {
                        if (eventWaitHandle.WaitOne())
                        {
                            var _dbContext = Services.BuildServiceProvider().GetRequiredService<AppDbContext>();
                            _dbContext.Database.EnsureCreated();
                            
                            var path = _dbContext.AppEvents.AsNoTracking().FirstOrDefault()?.Details;

                            _dbContext.AppEvents.ExecuteDelete();
                            
                            MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    HandleTextFile(path ?? "");
                                });
                        }
                    }
                }
            });
        }
        
        private void HandleTextFile(string filePath)
        {
            GlobalEvents.ProcessFolderContextMenuClick(filePath);
        }

        protected override void CleanUp()
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.CleanUp();
        }
    }
}
