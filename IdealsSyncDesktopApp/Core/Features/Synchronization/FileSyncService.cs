﻿using Microsoft.EntityFrameworkCore;

namespace IdealsSyncDesktopApp.Core;

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

        var dbFileIds = dbFiles.Where(x => x.SyncedFileId is not null).Select(x => x.SyncedFileId).ToList();
        var serverFilesToCreate = serverFiles.ExceptBy(dbFileIds, f => f.Id);
        var serverFilesToDelete = dbFiles.Where(x => x.SyncedFileId is not null)
            .ExceptBy(serverFiles.Select(x => x.Id), x => x.SyncedFileId!.Value).ToList();

        foreach (var fileToDelete in serverFilesToDelete)
        {
            fileToDelete.Status = SyncStatus.WaitingForDeletion;
        }

        await db.SaveChangesAsync();
        NotifyChanges();


        foreach (var fileToDelete in serverFilesToDelete)
        {
            try
            {
                if (File.Exists(fileToDelete.FullPath))
                {
                    File.Delete(fileToDelete.FullPath);
                }
                db.Files.Remove(fileToDelete);
                await db.SaveChangesAsync();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        dbFiles = db.Files.Include(x => x.History).Where(x => x.SyncPath == syncPath).ToList();

        // Get local files
        var localFiles = GetLocalFileNames(syncPath);

        var localFileNames = localFiles.Select(x => x.Name).ToList();

        var dbFileNames = dbFiles.Select(x => x.Name).ToList();
        var missingFileNames = dbFileNames.Except(localFileNames);



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

            try
            {
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
                        FileContent = new FileContent
                        {
                            Content = fileContentFromServer,
                            HistoryId = f.DbFile.CurrentVersion,
                        },
                        Id = f.DbFile.CurrentVersion,
                        File = f.DbFile,
                        Size = fileContentFromServer.LongLength,
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
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
           
        }
        
        var changedFiles = dbFiles.Where(dbFile =>
        {
            var localFile = localFiles.FirstOrDefault(x => string.Equals(x.Name, dbFile.Name, StringComparison.OrdinalIgnoreCase));

            string d = "";
            d.Trim(' ');
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
                    FileContent = new FileContent
                    {
                        Content = content,
                        HistoryId = changedFile.CurrentVersion
                    },
                    Size = content.LongLength,
                    Id = changedFile.CurrentVersion,
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

            try
            {
                var createdServerFile = await httpClient.UploadFileAsync(changedFile.Name, content);
                changedFile.LastUpdatedServerTime = createdServerFile.UpdatedAt;
                changedFile.SyncedFileId = createdServerFile.Id;
                changedFile.Status = SyncStatus.Synced;
                changedFile.SyncedAt = DateTime.Now;

                await db.SaveChangesAsync();
                NotifyChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
           

        }

        await db.SaveChangesAsync();
    
    

        foreach (var serverFile in stubFiles)
        {
            try
            {
                await SyncServerFileToLocalSystem(serverFile, db, serverFiles);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
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
            FileContent = new FileContent
            {
                Content = fileContent,
                HistoryId = localFile.CurrentVersion
            },
            Size = fileContent.LongLength,
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
                    Size = content.LongLength,
                    Id = currentVersionId,
                    FileContent = new FileContent
                    {
                        HistoryId = currentVersionId,
                        Content = content
                    },
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