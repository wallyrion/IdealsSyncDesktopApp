namespace BlazorHybridApp.Core;

public class GlobalEvents
{
    private static GlobalEvents _instance;

    private readonly IServiceProvider _di;


    public static event Action AppWasClosed;
    public static event Action<string> FodlerContextMenuClicked;


    public static void CloseApp()
    {
        AppWasClosed.Invoke();
    }

    public static void ProcessFolderContextMenuClick(string path)
    {
        FodlerContextMenuClicked.Invoke(path);
    }

    public GlobalEvents(IServiceProvider di)
    {
        _di = di;
        _instance = this;

        FodlerContextMenuClicked += HandleSyncWithIdealsContextAction;
    }

    public void Initialize()
    {

    }


    public async void HandleSyncWithIdealsContextAction(string path)
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
            Console.WriteLine("HandleSyncWithIdealsContextAction error "+ e.ToString());
        }
    }
}