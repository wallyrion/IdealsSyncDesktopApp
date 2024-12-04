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
            .Select(f => new FileInfo(f).Name)
            .ToList();
        var dbFileNames = db.Files.Select(x => x.Name).ToList();
        var missingFileNames = dbFileNames.Except(localFileNames);

        var dbFileIds = db.Files.Select(x => x.SyncedFileId).ToList();
        var serverFilesToCreate = serverFiles.ExceptBy(dbFileIds, f => f.Id);

        // sync files from server that does no exist locally
        foreach (var serverFile in serverFilesToCreate)
        {
            await SyncServerFileToLocalSystem(serverFile, db);
        }

        // sync files from local system that does not exist on the server
        var localFilesToCreate = localFileNames.Except(dbFileNames);

        var filesProjections = localFilesToCreate.Select(ConstructLocalFile).ToList();
        db.Files.AddRange(filesProjections);
        await db.SaveChangesAsync();

        // find deleted files
        var filesToDelete = db.Files.Where(x => missingFileNames.Contains(x.Name)).ToList();
        filesToDelete.ForEach(x => x.Status = SyncStatus.WaitingForDeletion);
        await db.SaveChangesAsync();

        foreach (var file in filesProjections)
        {
            await SyncLocalFileToServer(file, db);
        }
        
        foreach (var file in filesToDelete)
        {
            await RemoveFileFromServer(file, db);
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
            Status = SyncStatus.Synced,
            LastUpdatedAt = fileInfo.LastWriteTimeUtc
        });

        await db.SaveChangesAsync();
    }

    private async Task SyncLocalFileToServer(LocalFile file, AppDbContext db)
    {
        var path = Path.Combine(folderSelector.SyncPath!, file.Name);
        var bytes = await File.ReadAllBytesAsync(path);
        var createdServerFile = await httpClient.UploadFileAsync(file.Name, bytes);
        file.SyncedFileId = createdServerFile.Id;
        file.Status = SyncStatus.Synced;
        file.SyncedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private async Task RemoveFileFromServer(LocalFile file, AppDbContext db)
    {
        await httpClient.DeleteFileAsync(file.SyncedFileId);

        db.Files.Remove(file);
        await db.SaveChangesAsync();
    }

    private LocalFile ConstructLocalFile(string fileName)
    {
        var localFilePath = Path.Combine(folderSelector.SyncPath!, fileName);
        var fileInfo = new FileInfo(localFilePath);

        return new LocalFile
        {
            Id = Guid.NewGuid(),
            Name = fileName,
            LastUpdatedAt = fileInfo.LastWriteTimeUtc,
            Status = SyncStatus.SyncInProgress
        };
    }
}