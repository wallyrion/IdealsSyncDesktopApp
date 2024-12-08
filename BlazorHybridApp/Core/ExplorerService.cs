
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorHybridApp.Core;

public class ExplorerService(IServiceProvider serviceProvider, UserSettingsProvider userSettingsProvider, State state)
{
    public async Task<List<LocalFile>> GetAllFiles()
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        return await db.Files.Include(x => x.History).Where(x => x.SyncPath == userSettingsProvider.SyncPath).ToListAsync();
    }

    public async Task RevertFileVersion(FileHistoryItem item)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();


        var file = await db.Files.FirstAsync(x => x.Id == item.FileId);

        try
        {
            await state.ReadFileContentLock.WaitAsync();
            file.CurrentVersion = item.Id;
            file.CurrentHash = HashHelper.ComputeHash(item.Content);
            await db.SaveChangesAsync();
            await File.WriteAllBytesAsync(file.FullPath, item.Content);
        }
        catch (Exception e)
        {
            Console.WriteLine("RevertFileVersion", e);
        }
        finally
        {
            state.ReadFileContentLock.Release();
        }

        state.NotifyNewChanges();
    }
}