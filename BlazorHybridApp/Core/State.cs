using System;

namespace BlazorHybridApp.Core;

public static class AppGlobalState
{
    public static event Action AppWasClosed;

    public static void CloseApp()
    {
        AppWasClosed.Invoke();
    }
}

public class State
{
    public string CurrentUserEmail { get; set; } = "wally@gmail.com";
    
    public event Action? SyncStatusChanged;

    public SemaphoreSlim ReadFileContentLock = new(1, 1);

    public void NotifyNewChanges()
    {
        SyncStatusChanged?.Invoke();
    }
}