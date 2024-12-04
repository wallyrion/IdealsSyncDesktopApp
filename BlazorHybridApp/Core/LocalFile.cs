using System;

namespace BlazorHybridApp.Core;

public class LocalFile
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int SyncedFileId { get; set; }
    public DateTime? SyncedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}