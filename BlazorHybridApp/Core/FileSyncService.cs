using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorHybridApp.Core;

public class FileSyncService(FileSyncHttpClient httpClient, FolderSelector folderSelector, IServiceProvider serviceProvider)
{
    // private string SyncPath => FolderSelector.SyncPath;

    public async Task SyncAsync()
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        if (folderSelector.SyncPath is null)
        {
            return;
        }

        // Get server files
        var serverFiles = await httpClient.GetFilesAsync();

        // Get local files
        var localFileNames = Directory.GetFiles(folderSelector.SyncPath)
            .Select(f => new FileInfo(f).Name);

        var dbFileIds = db.Files.Select(x => x.SyncedFileId).ToList();
        var serverFilesToCreate = serverFiles.ExceptBy(dbFileIds, f => f.Id);

        // sync files from server that does no exist locally
        foreach (var serverFile in serverFilesToCreate)
        {
            await SyncServerFileToLocalSystem(serverFile, db);
        }

        // sync files from local system that does not exist on the server
        var dbFileNames = db.Files.Select(x => x.Name);
        var localFilesToCreate = localFileNames.Except(dbFileNames);

        foreach (var fileName in localFilesToCreate)
        {
            await SyncLocalFileToServer(fileName, db);
        }
    }

    private async Task SyncServerFileToLocalSystem(ServerFile serverFile, AppDbContext db)
    {
        var fileContent = await httpClient.DownloadFileAsync(serverFile.Id);
        var localFilePath = Path.Combine(folderSelector.SyncPath!, serverFile.Name);
        await File.WriteAllBytesAsync(localFilePath, fileContent);
        var fileInfo = new FileInfo(localFilePath);
        db.Files.Add(new LocalFile
        {
            Id = Guid.NewGuid(),
            Name = serverFile.Name,
            SyncedFileId = serverFile.Id,
            SyncedAt = DateTime.UtcNow,
            LastUpdatedAt = fileInfo.LastWriteTimeUtc
        });

        await db.SaveChangesAsync();
    }

    private async Task SyncLocalFileToServer(string fileName, AppDbContext db)
    {
        var localFilePath = Path.Combine(folderSelector.SyncPath!, fileName);
        var fileInfo = new FileInfo(localFilePath);

        var newFile = new LocalFile
        {
            Id = Guid.NewGuid(),
            Name = fileName,
            LastUpdatedAt = fileInfo.LastWriteTimeUtc
        };

        db.Files.Add(newFile);
        await db.SaveChangesAsync();

        var bytes = await File.ReadAllBytesAsync(localFilePath);
        var createdServerFile = await httpClient.UploadFileAsync(fileName, bytes);
        newFile.SyncedFileId = createdServerFile.Id;
        newFile.SyncedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}