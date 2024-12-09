namespace BlazorHybridApp.Core;

public class GlobalEvents
{
    private static GlobalEvents _instance;

    private readonly IServiceProvider _di;


    public static event Action AppWasClosed;
    public static event Action<string> FodlerContextMenuClicked;
    public static event Action<string> FileContextMenuClicked;


    public static void CloseApp()
    {
        AppWasClosed.Invoke();
    }

    public static void ProcessFolderContextMenuClick(string path)
    {
        if (IsFile(path))
        {
            Console.WriteLine("The path is a file.");
            FileContextMenuClicked.Invoke(path);

        }
        else if (IsFolder(path))
        {
            FodlerContextMenuClicked.Invoke(path);

            Console.WriteLine("The path is a folder.");
        }
        else
        {
            Console.WriteLine("The path is invalid or does not exist.");
        }
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

        FodlerContextMenuClicked += HandleFolderSyncWithIdealsContextAction;
        FileContextMenuClicked += HandleFileSyncWithIdealsContextAction;
    }

    public void Initialize()
    {
    }


    public async void HandleFolderSyncWithIdealsContextAction(string path)
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

    public async void HandleFileSyncWithIdealsContextAction(string path)
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