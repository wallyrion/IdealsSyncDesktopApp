using Microsoft.EntityFrameworkCore;

namespace BlazorHybridApp.Core;

public class FileContent
{
    public FileHistoryItem History { get; set; }
    public Guid HistoryId { get; set; }
    public required byte[] Content { get; set; }
}

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppState> AppStates { get; set; }
    public DbSet<LocalFile> Files { get; set; }
    
    public DbSet<FileHistoryItem> History { get; set; }
    public DbSet<FileContent> Contents { get; set; }
    public DbSet<AppEvent> AppEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LocalFile>()
            .HasMany(x => x.History)
            .WithOne(x => x.File);

        modelBuilder.Entity<FileContent>()
            .HasKey(x => x.HistoryId);
            
        
        modelBuilder.Entity<FileContent>()
            .HasOne(x => x.History)
            .WithOne(x => x.FileContent);
    }
}

public class FileHistoryItem
{
    public required Guid Id { get; init; }
    public required DateTime ModifiedAt { get; init; }
    public required string MofifiedBy { get; init; }
    public Guid FileId { get; set; }
    public LocalFile File { get; set; }
    public FileContent FileContent { get; set; }
    public required long Size { get; set; }
}

public class AppEvent
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Details { get; set; }
}