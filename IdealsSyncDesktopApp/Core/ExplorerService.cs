using Microsoft.EntityFrameworkCore;

namespace IdealsSyncDesktopApp.Core;

public class ExplorerService(IServiceProvider serviceProvider, UserSettingsProvider userSettingsProvider, State state)
{
    public async Task<List<LocalFile>> GetAllFiles()
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        return await db.Files
            .AsNoTracking()
            .Include(x => x.History)
            .OrderBy(x => x.Name)
            .Where(x => x.SyncPath == userSettingsProvider.SyncPath).ToListAsync();
    }

    public async Task RevertFileVersion(FileHistoryItem item)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var file = await db.Files.FirstAsync(x => x.Id == item.FileId);

        try
        {
            var content = await db.Contents.FirstOrDefaultAsync(x => x.HistoryId == item.Id);
            if (content == null)
            {
                return;
            }
            
            await state.ReadFileContentLock.WaitAsync();
            file.CurrentVersion = item.Id;
            file.CurrentHash = HashHelper.ComputeHash(content.Content);
            await db.SaveChangesAsync();
            await File.WriteAllBytesAsync(file.FullPath, content.Content);
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