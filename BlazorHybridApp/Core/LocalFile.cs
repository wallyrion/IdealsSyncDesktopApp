using System;

namespace BlazorHybridApp.Core;

public class LocalFile
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public int? SyncedFileId { get; set; }
    public DateTime? SyncedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    
    public SyncStatus Status { get; set; }
    public required string SyncPath { get; set; }
}


public enum SyncStatus
{
    SyncInProgress,
    Synced,
    WaitingForDeletion
}