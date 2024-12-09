using System.Diagnostics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System.IO.MemoryMappedFiles;
using System.Threading;
using IdealsSyncDesktopApp.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Platform;

namespace IdealsSyncDesktopApp
{
    public partial class App : Application
    {
        private static Mutex _mutex;
        private const string MutexName = "YourAppMutexName";
        private readonly ServiceCollection Services;
        
        public App()
        {
            //InitializeComponent();
            Console.WriteLine("Creating application");
            
            InitializeComponent();
            var services = new ServiceCollection();
            var mainModule = Process.GetCurrentProcess().MainModule;
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(LocationHelper.GetDbConnectionString()));
            Services = services;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            _mutex = new Mutex(true, MutexName, out bool createdNew);

            var provider = Services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureCreated();

            if (!createdNew)
            {
                // Another instance exists, send file path to it
                var args = Environment.GetCommandLineArgs();
                if (args.Length > 1)
                {
                    
                    dbContext.AppEvents.Add(new AppEvent
                    {
                        Id = Guid.NewGuid(),
                        Details = args[1],
                        Name = "Requested from explorer",
                        CreatedAt = DateTime.Now
                    });

                    dbContext.SaveChanges();
                
                    // Signal the existing instance
                    using (var eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "IdealsAppEvent"))
                    {
                        eventWaitHandle.Set();
                    }
                
                    Environment.Exit(0);
                    return null;
                }
            }
            else
            {
                try
                {
                    RegistryHelper.RegisterOpenWithIdeals();
                }
                catch (Exception e)
                {
                    Debug.Fail(e.ToString());
                    Console.WriteLine(e);
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
                using var eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "IdealsAppEvent");
                while (true)
                {
                    if (eventWaitHandle.WaitOne())
                    {
                        using var dbContext = Services.BuildServiceProvider().GetRequiredService<AppDbContext>();

                        var appEvent = dbContext.AppEvents.OrderBy(x => x.ProcessedAt).FirstOrDefault();

                        if (appEvent == null)
                        {
                            dbContext.AppEvents.Add(new AppEvent
                            {
                                Name = "IdealsAppEvent processing skipped",
                                Id = Guid.NewGuid(),
                                CreatedAt = DateTime.Now,
                                ProcessedAt = DateTime.Now,
                                Details = "IdealsAppEvent triggered for unknown reason"
                            });
                        }
                        else
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                GlobalEvents.ProcessContextMenuAction(appEvent.Id);

                                //HandleTextFile(path ?? "");
                            });
                        }

                        
                    }
                }
            });
        }
        
        private void HandleTextFile(string filePath)
        {
            
        }

        protected override void CleanUp()
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.CleanUp();
        }
    }
}
