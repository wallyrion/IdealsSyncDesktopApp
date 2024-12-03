using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorHybridApp.Components;

public class FileSyncService(FileSyncHttpClient httpClient, FolderSelector folderSelector)
{
   // private string SyncPath => FolderSelector.SyncPath;

   public async Task SyncAsync()
    {
        // Get server files
        var serverFiles = await httpClient.GetFilesAsync();

        // Get local files
        var localFiles = Directory.GetFiles(folderSelector.SyncPath)
            .Select(f => new FileInfo(f))
            .ToDictionary(f => f.Name, f => f);

        // 1) Create file locally if it exists on the server but not locally
        foreach (var serverFile in serverFiles)
        {
            var localFilePath = Path.Combine(folderSelector.SyncPath, serverFile.Name);

            if (!localFiles.ContainsKey(serverFile.Name))
            {
                // Download file from the server
                var content = await httpClient.DownloadFileAsync(serverFile.Id);
                Console.WriteLine($"Downloaded: {serverFile.Name}");
                await File.WriteAllBytesAsync(localFilePath, content);
            }

            // Remove from localFiles dictionary to avoid duplicate processing in the next step
            //localFiles.Remove(serverFile.Name);
            //Console.WriteLine($"Removed: {serverFile.Name}");
        }

        // 2) Create file on the server if it exists locally but not on the server
        var serverFileNames = new HashSet<string>(serverFiles.Select(f => f.Name));
        foreach (var localFile in localFiles.Values)
        {
            if (!serverFileNames.Contains(localFile.Name))
            {
                // Upload file to the server
                await httpClient.UploadFileAsync(localFile.FullName);
                Console.WriteLine($"Uploaded: {localFile.Name}");
            }
        }
    }
}