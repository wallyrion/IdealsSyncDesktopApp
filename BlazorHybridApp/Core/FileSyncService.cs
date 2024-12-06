using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorHybridApp.Core;

public class FileSyncService(FileSyncHttpClient httpClient, FolderSelector folderSelector, IServiceProvider serviceProvider, State state)
{
    public async Task SyncAsync()
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (folderSelector.SyncPath is null)
        {
            return;
        }

        var syncPath = folderSelector.SyncPath;
        // Get server files
        var serverFiles = await httpClient.GetFilesAsync();
        var dbFiles = db.Files.Include(x => x.History).Where(x => x.SyncPath == syncPath).ToList();

        // Get local files
        var localFiles = Directory.GetFiles(folderSelector.SyncPath)
            .Select(f => new FileInfo(f))
            .ToList();

        var localFileNames = localFiles.Select(x => x.Name).ToList();

        var dbFileNames = dbFiles.Select(x => x.Name).ToList();
        var missingFileNames = dbFileNames.Except(localFileNames);

        var dbFileIds = dbFiles.Select(x => x.SyncedFileId).ToList();
        var serverFilesToCreate = serverFiles.ExceptBy(dbFileIds, f => f.Id);

        // sync files from server that does not exist locally
        var stubFiles = serverFilesToCreate.Select(x => ConstructStubLocalFile(x, syncPath)).ToList();
        db.Files.AddRange(stubFiles);
        await db.SaveChangesAsync();
        NotifyChanges();

        var changedFiles = dbFiles.Where(dbFile =>
        {
            var localFile = localFiles.FirstOrDefault(x => string.Equals(x.Name, dbFile.Name, StringComparison.OrdinalIgnoreCase));

            if (localFile == null)
            {
                return false;
            }

            return localFile.LastWriteTime > dbFile.LastUpdatedAt;
        }).ToList();

        foreach (var changedFile in changedFiles)
        {
            changedFile.Status = SyncStatus.OutgoingSync;
            var content = await File.ReadAllBytesAsync(changedFile.FullPath);
            db.History.Add(new FileHistoryItem
            {
                Id = Guid.NewGuid(),
                Content = content,
                ModifiedAt = DateTime.Now,
                MofifiedBy = state.CurrentUserEmail,
                File = changedFile
            });
        }

        await db.SaveChangesAsync();
        NotifyChanges();

        foreach (var changedFile in changedFiles)
        {
            await ReUploadFileToServer(changedFile, db, syncPath);
        }

        foreach (var serverFile in stubFiles)
        {
            await SyncServerFileToLocalSystem(serverFile, db);
        }

        // sync files from local system that does not exist on the server
        var localFilesToCreate = localFileNames.Except(dbFileNames);

        var filesProjections = localFilesToCreate.Select(x => ConstructLocalFile(x, syncPath)).ToList();
        db.Files.AddRange(filesProjections);
        await db.SaveChangesAsync();
        NotifyChanges();

        // find deleted files
        var filesToDelete = dbFiles.Where(x => missingFileNames.Contains(x.Name)).ToList();
        filesToDelete.ForEach(x => x.Status = SyncStatus.WaitingForDeletion);
        await db.SaveChangesAsync();
        NotifyChanges();

        foreach (var file in filesProjections)
        {
            await SyncLocalFileToServer(file, db, syncPath);
        }

        foreach (var file in filesToDelete)
        {
            await RemoveFileFromServer(file, db);
            NotifyChanges();
        }
    }

    private void NotifyChanges()
    {
        state.NotifyNewChanges();
    }

    private async Task SyncServerFileToLocalSystem(LocalFile localFile, AppDbContext db)
    {
        await Task.Delay(1000);
        var fileContent = await httpClient.DownloadFileAsync(localFile.SyncedFileId!.Value);
        var localFilePath = Path.Combine(localFile.SyncPath, localFile.Name);
        await File.WriteAllBytesAsync(localFilePath, fileContent);
        var fileInfo = new FileInfo(localFilePath);

        localFile.LastUpdatedAt = fileInfo.LastWriteTime;
        localFile.SyncedAt = DateTime.Now;
        localFile.Status = SyncStatus.Synced;

        db.History.Add(new FileHistoryItem
        {
            Content = fileContent,
            Id = Guid.NewGuid(),
            ModifiedAt = localFile.LastUpdatedAt.Value,
            MofifiedBy = state.CurrentUserEmail,
            File = localFile
        });

        await db.SaveChangesAsync();
        NotifyChanges();
    }

    private LocalFile ConstructStubLocalFile(ServerFile serverFile, string syncPath)
    {
        return new LocalFile
        {
            Id = Guid.NewGuid(),
            Name = serverFile.Name,
            SyncPath = syncPath,
            SyncedFileId = serverFile.Id,
            Status = SyncStatus.IncomingSync
        };
    }

    private async Task SyncLocalFileToServer(LocalFile file, AppDbContext db, string syncPath)
    {
        await Task.Delay(1000);
        var path = Path.Combine(folderSelector.SyncPath!, file.Name);
        var bytes = await File.ReadAllBytesAsync(path);
        var createdServerFile = await httpClient.UploadFileAsync(file.Name, bytes);
        file.SyncedFileId = createdServerFile.Id;
        file.Status = SyncStatus.Synced;
        file.SyncPath = syncPath;
        file.SyncedAt = DateTime.Now;
        await db.SaveChangesAsync();
        NotifyChanges();
    }

    private async Task ReUploadFileToServer(LocalFile file, AppDbContext db, string syncPath)
    {
        if (file.SyncedFileId is not null)
        {
            await httpClient.DeleteFileAsync(file.SyncedFileId.Value);
        }

        var localFilePath = Path.Combine(folderSelector.SyncPath!, file.Name);
        var fileInfo = new FileInfo(localFilePath);
    
        await Task.Delay(1000);
        var path = Path.Combine(folderSelector.SyncPath!, file.Name);
        var bytes = await File.ReadAllBytesAsync(path);
        var createdServerFile = await httpClient.UploadFileAsync(file.Name, bytes);
        file.SyncedFileId = createdServerFile.Id;
        file.Status = SyncStatus.Synced;
        file.SyncPath = syncPath;
        file.SyncedAt = DateTime.Now;
        file.LastUpdatedAt = fileInfo.LastWriteTime;
        await db.SaveChangesAsync();
        NotifyChanges();
    }

    private async Task RemoveFileFromServer(LocalFile file, AppDbContext db)
    {
        await Task.Delay(1000);
        if (file.SyncedFileId is not null)
        {
            await httpClient.DeleteFileAsync(file.SyncedFileId.Value);
        }

        db.Files.Remove(file);
        await db.SaveChangesAsync();
    }

    private LocalFile ConstructLocalFile(string fileName, string syncPath)
    {
        var localFilePath = Path.Combine(folderSelector.SyncPath!, fileName);
        var fileInfo = new FileInfo(localFilePath);

        return new LocalFile
        {
            Id = Guid.NewGuid(),
            Name = fileName,
            LastUpdatedAt = fileInfo.LastWriteTime,
            Status = SyncStatus.OutgoingSync,
            SyncPath = syncPath,
            History =
            [
                new()
                {
                    Id = Guid.NewGuid(),
                    Content = File.ReadAllBytes(localFilePath),
                    ModifiedAt = fileInfo.LastWriteTime,
                    MofifiedBy = state.CurrentUserEmail
                }
            ]
        };
    }

    public async Task RemoveFileLocally(LocalFile file)
    {
        var path = Path.Combine(file.SyncPath, file.Name);
        File.Delete(path);
    }
}