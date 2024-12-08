using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorHybridApp.Core;

public class FileSyncService(FileSyncHttpClient httpClient, UserSettingsProvider userSettingsProvider, IServiceProvider serviceProvider, State state)
{
    public async Task SyncAsync()
    {
        var userSettings = await userSettingsProvider.GetUserSettingsAsync();
        if (userSettings is null)
        {
            // setup not completed
            return;
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (userSettingsProvider.SyncPath is null)
        {
            return;
        }

        var syncPath = userSettingsProvider.SyncPath;
        // Get server files
        var serverFiles = await httpClient.GetFilesAsync();
        var dbFiles = db.Files.Include(x => x.History).Where(x => x.SyncPath == syncPath).ToList();

        // Get local files
        var localFiles = GetLocalFileNames(syncPath);

        var localFileNames = localFiles.Select(x => x.Name).ToList();

        var dbFileNames = dbFiles.Select(x => x.Name).ToList();
        var missingFileNames = dbFileNames.Except(localFileNames);

        var dbFileIds = dbFiles.Select(x => x.SyncedFileId).ToList();
        var serverFilesToCreate = serverFiles.ExceptBy(dbFileIds, f => f.Id);

        // sync files from server that does not exist locally
        var stubFiles = serverFilesToCreate.Select(x => ConstructStubLocalFile(x, syncPath)).ToList();
        db.Files.AddRange(stubFiles);
        await db.SaveChangesAsync();

        if (stubFiles.Any())
        {
            NotifyChanges();
        }

        var serverFilesToUpdate = dbFiles.Join(serverFiles, x => x.SyncedFileId, x => x.Id, (dbFile, serverFile) => new
        {
            DbFile = dbFile,
            ServerFile = serverFile
        }).Where(x => x.ServerFile.UpdatedAt > x.DbFile.LastUpdatedServerTime).ToList();

        foreach (var f in serverFilesToUpdate)
        {
            f.DbFile.Status = SyncStatus.IncomingSync;
            await db.SaveChangesAsync();
            NotifyChanges();
            
            var fileContentFromServer = await httpClient.DownloadFileAsync(f.ServerFile.Id);
            var hash = HashHelper.ComputeHash(fileContentFromServer);

            try
            {
                await state.ReadFileContentLock.WaitAsync();
                f.DbFile.CurrentHash = hash;
                f.DbFile.CurrentVersion = Guid.NewGuid();
                f.DbFile.LastModifiedBy = f.ServerFile.LastModifiedBy;
                f.DbFile.LastUpdatedServerTime = f.ServerFile.UpdatedAt;
                var fileMetadata = new FileInfo(f.DbFile.FullPath);
                await File.WriteAllBytesAsync(f.DbFile.FullPath, fileContentFromServer);
                
                db.History.Add(new FileHistoryItem
                {
                    Id = f.DbFile.CurrentVersion,
                    File = f.DbFile,
                    Content = fileContentFromServer,
                    MofifiedBy = f.ServerFile.LastModifiedBy,
                    ModifiedAt = fileMetadata.LastWriteTime
                });

                f.DbFile.Status = SyncStatus.Synced;
                await db.SaveChangesAsync();
                NotifyChanges();
            }
            finally
            {
                state.ReadFileContentLock.Release();
            }
        }
        
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
            byte[] content = null;
            try
            {
                await state.ReadFileContentLock.WaitAsync();

                var fileInfo = new FileInfo(changedFile.FullPath);
                content = await File.ReadAllBytesAsync(changedFile.FullPath);
                var hash = HashHelper.ComputeHash(content);


                if (changedFile.CurrentHash == hash)
                {
                    changedFile.LastUpdatedAt = fileInfo.LastWriteTime;
                    await db.SaveChangesAsync();
                    continue;
                }

                changedFile.Status = SyncStatus.OutgoingSync;
                changedFile.CurrentVersion = Guid.NewGuid();
                changedFile.CurrentHash = hash;
                changedFile.LastModifiedBy = state.CurrentUserEmail;
                changedFile.LastUpdatedAt = fileInfo.LastWriteTime;
                db.History.Add(new FileHistoryItem
                {
                    Id = changedFile.CurrentVersion,
                    Content = content,
                    ModifiedAt = DateTime.Now,
                    MofifiedBy = state.CurrentUserEmail,
                    File = changedFile
                });
                await db.SaveChangesAsync();
                NotifyChanges();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                state.ReadFileContentLock.Release();
            }

            /*if (changedFile.SyncedFileId is not null)
            {
                await httpClient.DeleteFileAsync(changedFile.SyncedFileId.Value);
            }*/

            if (content == null)
            {
                return;
            }

            var createdServerFile = await httpClient.UploadFileAsync(changedFile.Name, content);
            changedFile.LastUpdatedServerTime = createdServerFile.UpdatedAt;
            changedFile.SyncedFileId = createdServerFile.Id;
            changedFile.Status = SyncStatus.Synced;
            changedFile.SyncedAt = DateTime.Now;

            await db.SaveChangesAsync();
            NotifyChanges();

        }

        await db.SaveChangesAsync();
    
    

        foreach (var serverFile in stubFiles)
        {
            await SyncServerFileToLocalSystem(serverFile, db, serverFiles);
        }

        // sync files from local system that does not exist on the server
        var localFilesToCreate = localFileNames.Except(dbFileNames);

        var filesProjections = localFilesToCreate.Select(x => ConstructLocalFile(x, syncPath)).ToList();
        db.Files.AddRange(filesProjections);
        await db.SaveChangesAsync();
        if (filesProjections.Any())
        {
            NotifyChanges();
        }

        // find deleted files
        var filesToDelete = dbFiles.Where(x => missingFileNames.Contains(x.Name)).ToList();
        filesToDelete.ForEach(x => x.Status = SyncStatus.WaitingForDeletion);
        await db.SaveChangesAsync();
        if (filesToDelete.Any())
        {
            NotifyChanges();
        }

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

    private async Task SyncServerFileToLocalSystem(LocalFile localFile, AppDbContext db, List<ServerFile> serverFiles)
    {
        var serverFileMetadata = serverFiles.First(x => x.Id == localFile.SyncedFileId);
        var fileContent = await httpClient.DownloadFileAsync(localFile.SyncedFileId!.Value);
        var localFilePath = Path.Combine(localFile.SyncPath, localFile.Name);
        await File.WriteAllBytesAsync(localFilePath, fileContent);
        var fileInfo = new FileInfo(localFilePath);

        localFile.LastUpdatedAt = fileInfo.LastWriteTime;
        localFile.SyncedAt = DateTime.Now;
        localFile.Status = SyncStatus.Synced;
        localFile.CurrentVersion = Guid.NewGuid();
        localFile.CurrentHash = HashHelper.ComputeHash(fileContent);
        localFile.LastUpdatedServerTime = serverFileMetadata.UpdatedAt;

        db.History.Add(new FileHistoryItem
        {
            Content = fileContent,
            Id = localFile.CurrentVersion,
            ModifiedAt = localFile.LastUpdatedAt.Value,
            MofifiedBy = localFile.LastModifiedBy,
            File = localFile
        });

        await db.SaveChangesAsync();
        NotifyChanges();
    }

    private List<FileInfo> GetLocalFileNames(string syncPath)
    {
        var localFiles =
            Directory.GetFiles(syncPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => !Path.GetFileName(f).StartsWith("~$"))
                .Select(f => new FileInfo(f))
                .ToList();
        ;

        return localFiles;
    }

    private LocalFile ConstructStubLocalFile(ServerFile serverFile, string syncPath)
    {
        return new LocalFile
        {
            Id = Guid.NewGuid(),
            Name = serverFile.Name,
            SyncPath = syncPath,
            LastModifiedBy = serverFile.LastModifiedBy,
            SyncedFileId = serverFile.Id,
            Status = SyncStatus.IncomingSync
        };
    }

    private async Task SyncLocalFileToServer(LocalFile file, AppDbContext db, string syncPath)
    {
        try
        {
            var path = Path.Combine(syncPath, file.Name);
            var bytes = await File.ReadAllBytesAsync(path);
            var createdServerFile = await httpClient.UploadFileAsync(file.Name, bytes);
            file.SyncedFileId = createdServerFile.Id;
            file.Status = SyncStatus.Synced;
            file.SyncPath = syncPath;
            file.SyncedAt = DateTime.Now;
            await db.SaveChangesAsync();
            NotifyChanges();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);

        }
        
    }

    private async Task ReUploadFileToServer(LocalFile file, AppDbContext db, string syncPath)
    {
        
    }

    private async Task RemoveFileFromServer(LocalFile file, AppDbContext db)
    {
        if (file.SyncedFileId is not null)
        {
            await httpClient.DeleteFileAsync(file.SyncedFileId.Value);
        }

        db.Files.Remove(file);
        await db.SaveChangesAsync();
    }

    private LocalFile ConstructLocalFile(string fileName, string syncPath)
    {
        var localFilePath = Path.Combine(userSettingsProvider.SyncPath!, fileName);
        var fileInfo = new FileInfo(localFilePath);

        var currentVersionId = Guid.NewGuid();
        var content = File.ReadAllBytes(localFilePath);
        var currentHash = HashHelper.ComputeHash(content);
        return new LocalFile
        {
            Id = Guid.NewGuid(),
            Name = fileName,
            LastUpdatedAt = fileInfo.LastWriteTime,
            Status = SyncStatus.OutgoingSync,
            SyncPath = syncPath,
            CurrentHash = currentHash,
            CurrentVersion = currentVersionId,
            LastModifiedBy = state.CurrentUserEmail,
            History =
            [
                new()
                {
                    Id = currentVersionId,
                    Content = content,
                    ModifiedAt = fileInfo.LastWriteTime,
                    MofifiedBy = state.CurrentUserEmail,
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