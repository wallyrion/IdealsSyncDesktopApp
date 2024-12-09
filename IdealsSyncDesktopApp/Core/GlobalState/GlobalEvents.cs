using Microsoft.EntityFrameworkCore;

namespace IdealsSyncDesktopApp.Core;

public class GlobalEvents
{
    private static GlobalEvents _instance;

    private readonly IServiceProvider _di;


    public static event Action AppWasClosed;
    public static event Action<Guid> ContextMenuTriggered;

    public static void ProcessContextMenuAction(Guid eventId)
    {
        ContextMenuTriggered.Invoke(eventId);
    }

    public static void CloseApp()
    {
        AppWasClosed.Invoke();
    }


    public static bool IsFile(string path)
    {
        return File.Exists(path);
    }

    public static bool IsFolder(string path)
    {
        return Directory.Exists(path);
    }

    

    public GlobalEvents(IServiceProvider di)
    {
        _di = di;
        _instance = this;

        ContextMenuTriggered += HandleContextMenuAction;
    }

    public void Initialize()
    {
    }


    public async Task HandleFolderSyncWithIdealsContextAction(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }


            await using var scope = _di.CreateAsyncScope();

            var folderSelector = scope.ServiceProvider.GetRequiredService<UserSettingsProvider>();
            var state = scope.ServiceProvider.GetRequiredService<State>();

            await folderSelector.SelectNewSyncPathAsync(path);
            state.NotifyNewChanges();
        }
        catch (Exception e)
        {
            Console.WriteLine("HandleFolderSyncWithIdealsContextAction error " + e.ToString());
        }
    }

    public async void HandleContextMenuAction(Guid eventId)
    {
        try
        {
            await using var scope = _di.CreateAsyncScope();

            var folderSelector = scope.ServiceProvider.GetRequiredService<UserSettingsProvider>();
            var state = scope.ServiceProvider.GetRequiredService<State>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var appEvent = await db.AppEvents.FirstAsync(x => x.Id == eventId);
            appEvent.ProcessedAt = DateTime.Now;
            await db.SaveChangesAsync();

            var path = appEvent.Details;

            if (IsFile(path))
            {
                await HandleFileSyncWithIdealsContextAction(path);

            }
            else if (IsFolder(path))
            {
                await HandleFolderSyncWithIdealsContextAction(path);
            }
            else
            {
                Console.WriteLine("The path is invalid or does not exist.");
            }

        }
        catch (Exception e)
        {
            Console.WriteLine("HandleFolderSyncWithIdealsContextAction error " + e.ToString());
        }
    }

    public async Task HandleFileSyncWithIdealsContextAction(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }


            await using var scope = _di.CreateAsyncScope();

            var settings = scope.ServiceProvider.GetRequiredService<UserSettingsProvider>();

            var syncPath = settings.SyncPath;

            if (syncPath == null || !Directory.Exists(syncPath))
            {
                Console.WriteLine("Sync path not valid");
                return;
            }

            var fileName = Path.GetFileName(path);
            var content = await File.ReadAllBytesAsync(path);
            await File.WriteAllBytesAsync(Path.Combine(syncPath, fileName), content);

        }
        catch (Exception e)
        {
            Console.WriteLine("HandleFolderSyncWithIdealsContextAction error " + e.ToString());
        }
    }
}