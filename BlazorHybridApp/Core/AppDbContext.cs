using Microsoft.EntityFrameworkCore;

namespace BlazorHybridApp.Core;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppState> AppStates { get; set; }
    public DbSet<LocalFile> Files { get; set; }
    
    public DbSet<FileHistoryItem> History { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LocalFile>()
            .HasMany(x => x.History)
            .WithOne(x => x.File);
    }
}

public class FileHistoryItem
{
    public required Guid Id { get; init; }
    public required DateTime ModifiedAt { get; init; }
    public required string MofifiedBy { get; init; }
    public required byte[] Content { get; init; } 
    public Guid FileId { get; set; }
    public LocalFile File { get; set; }
}