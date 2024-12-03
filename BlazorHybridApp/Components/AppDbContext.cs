using Microsoft.EntityFrameworkCore;

namespace BlazorHybridApp.Components;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppState> AppStates { get; set; }
}