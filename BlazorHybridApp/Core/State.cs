using System;

namespace BlazorHybridApp.Core;

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