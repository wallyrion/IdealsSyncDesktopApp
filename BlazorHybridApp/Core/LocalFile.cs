using System;

namespace BlazorHybridApp.Core;

public class LocalFile
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string LastModifiedBy { get; set; }
    public int? SyncedFileId { get; set; }
    public DateTime? SyncedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public DateTime? LastUpdatedServerTime { get; set; }
    
    public SyncStatus Status { get; set; }
    public required string SyncPath { get; set; }
    
    public List<FileHistoryItem> History { get; set; }
    public Guid CurrentVersion { get; set; }
    public string? CurrentHash { get; set; }
    /*public FileHistoryItem CurrentVersion { get; set; }
    public Guid CurrentVersionId { get; set; }*/


    public string FullPath => Path.Combine(SyncPath, Name);
}


public enum SyncStatus
{
    OutgoingSync,
    IncomingSync,
    Synced,
    WaitingForDeletion
}