using System;

namespace BlazorHybridApp.Core;

public class State
{
    public string CurrentUserEmail { get; set; } = "wally@gmail.com";
    
    public event Action? SyncStatusChanged;

    public void NotifyNewChanges() => SyncStatusChanged?.Invoke();
}