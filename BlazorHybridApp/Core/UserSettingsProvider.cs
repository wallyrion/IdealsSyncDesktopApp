using System.Text.Json;
using System.Threading.Tasks;
using BlazorHybridApp.Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace BlazorHybridApp.Core;

public class UserSettings
{
    public string? Email { get; set; }
    public string? SyncPath { get; set; }
}

public class UserSettingsProvider(IServiceProvider serviceProvider, State state)
{
    public string? SyncPath => UserSettings.SyncPath;
    public UserSettings UserSettings { get; set; }

    public async Task<UserSettings?> GetUserSettingsAsync()
    {
        await using var scope = serviceProvider.CreateDbContextScoped(out var dbContext);
        var userSettingsJson = dbContext.AppStates.FirstOrDefault(x => x.Key == "UserSettings");

        if (userSettingsJson is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(userSettingsJson.Value))
        {
            return null;
        }

        var userSettings = JsonSerializer.Deserialize<UserSettings>(userSettingsJson.Value);

        if (userSettings == null)
        {
            return null;
        }

        UserSettings = userSettings!;

        return userSettings;
    }

    public async Task UpdateUserSettingsAsync(UserSettings userSettings)
    {
        await using var scope = serviceProvider.CreateDbContextScoped(out var dbContext);

        var json = JsonSerializer.Serialize(userSettings);

        var userSettingsJson = dbContext.AppStates.FirstOrDefault(x => x.Key == "UserSettings");

        if (userSettingsJson is not null)
        {
            userSettingsJson.Value = json;
        }
        else
        {
            var userSettingsAppState = new AppState
            {
                Key = "UserSettings",
                Value = json
            };

            dbContext.AppStates.Add(userSettingsAppState);
        }

        await dbContext.SaveChangesAsync();
        UserSettings = userSettings;
        state.NotifyNewSettingsChanges();
    }

    public async Task SelectNewSyncPathAsync(string path)
    {
        var settings = await GetUserSettingsAsync();

        if (settings is null)
        {
            // first setup not completed
            return;
        }
        
        settings.SyncPath = path;

        await UpdateUserSettingsAsync(settings);
    }
}