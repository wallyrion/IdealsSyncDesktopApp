namespace IdealsSyncDesktopApp.Core;

public class State
{
    public string? CurrentUserEmail { get; set; } = "test@gmail.com";
    
    public event Action? SyncStatusChanged;
    public event Action? SettingsChanged;

    public SemaphoreSlim ReadFileContentLock = new(1, 1);

    public void NotifyNewChanges()
    {
        SyncStatusChanged?.Invoke();
    }   
    
    public void NotifyNewSettingsChanges()
    {
        SettingsChanged?.Invoke();
    }
}