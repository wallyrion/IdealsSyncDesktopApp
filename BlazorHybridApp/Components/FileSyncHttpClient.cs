using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlazorHybridApp.Components;

public class FileSyncHttpClient
{
    private readonly HttpClient _client;

    public FileSyncHttpClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<List<ServerFile>> GetFilesAsync(int page = 1, int pageSize = 20)
    {
        var response = await _client.GetAsync($"/files?page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<ServerFile>>();

        return result ?? [];
    }

    public async Task UploadFileAsync(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        using var content = new MultipartFormDataContent();

        var bytes = await File.ReadAllBytesAsync(filePath);

        var streamFromBytes = new MemoryStream(bytes);
        
        var streamContent = new StreamContent(streamFromBytes);

        var contentType = ContentTypeHelper.GetMimeType(Path.GetExtension(fileName));
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        var response = await _client.PostAsync("/files/upload", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
    }

    public async Task<byte[]> DownloadFileAsync(int fileId)
    {
        var response = await _client.GetAsync($"/files/download/{fileId}");
        response.EnsureSuccessStatusCode();
        var fileBytes = await response.Content.ReadAsByteArrayAsync();

        return fileBytes;
        
    }

    public async Task DeleteFileAsync(int fileId)
    {
        var response = await _client.DeleteAsync($"/files/{fileId}");
        response.EnsureSuccessStatusCode();
    }
}

public class ServerFile
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ContentType { get; set; }
    public long Size { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}