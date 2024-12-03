using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BlazorHybridApp.Components;

public class FolderSelector
{
    private readonly AppDbContext _dbContext;
    public string SyncPath { get; private set; }

    public FolderSelector(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    
    public async Task<string> RetrieveAndStoreSyncPath()
    {
        var state = await _dbContext.AppStates.FirstOrDefaultAsync(x => x.Key == "SyncPath");

        SyncPath = state?.Value!;

        return SyncPath;
    }

    public async Task SelectNewSyncPath(string path)
    {
        var state = await _dbContext.AppStates.FirstOrDefaultAsync(x => x.Key == "SyncPath");

        if (state == null)
        {
            state = new AppState
            {
                Key = "SyncPath",
                Value = path
            };

            _dbContext.AppStates.Add(state);
        }
        else
        {
            state.Value = path;
        }
        
        await _dbContext.SaveChangesAsync();

    }

}