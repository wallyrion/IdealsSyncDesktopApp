using Microsoft.EntityFrameworkCore;

namespace BlazorHybridApp.Core;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppState> AppStates { get; set; }
    public DbSet<LocalFile> Files { get; set; }
}