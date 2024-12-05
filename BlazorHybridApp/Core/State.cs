using System;

namespace BlazorHybridApp.Core;

public class State
{
    public event Action? SyncStatusChanged;

    public void NotifyNewChanges() => SyncStatusChanged?.Invoke();
}