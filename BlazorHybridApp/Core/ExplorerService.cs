
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorHybridApp.Core;

public class ExplorerService(IServiceProvider serviceProvider, FolderSelector folderSelector)
{
    public async Task<List<LocalFile>> GetAllFiles()
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        return await db.Files.Where(x => x.SyncPath == folderSelector.SyncPath).ToListAsync();
    }
}