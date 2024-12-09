using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BlazorHybridApp.Core;

public class FileSyncHttpClient
{
    private readonly HttpClient _client;
    private readonly State _state;
    private readonly UserSettingsProvider _userSettingsProvider;

    public FileSyncHttpClient(HttpClient client, State state, UserSettingsProvider userSettingsProvider)
    {
        _client = client;
        _state = state;
        _userSettingsProvider = userSettingsProvider;
    }

    public async Task<List<ServerFile>> GetFilesAsync(int page = 1)
    {
        var delay = await _userSettingsProvider.GetOperationDelayAsync();
        await Task.Delay(delay);
        
        var response = await _client.GetAsync($"/files?page={page}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<ServerFile>>();

        return result ?? [];
    }
    
    public async Task<ServerFile> UploadFileAsync(string fileName, byte[] fileContent)
    {
        var delay = await _userSettingsProvider.GetOperationDelayAsync();
        await Task.Delay(delay);
        
        var modifiedBy = _state.CurrentUserEmail;
        using var streamFromBytes = new MemoryStream(fileContent);
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(streamFromBytes);

        var contentType = ContentTypeHelper.GetMimeType(Path.GetExtension(fileName));
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        var response = await _client.PostAsync($"/files/upload?modifiedBy={modifiedBy}", content);
        response.EnsureSuccessStatusCode();
        var file = await response.Content.ReadFromJsonAsync<ServerFile>();

        return file;
    }

    public async Task<byte[]> DownloadFileAsync(int fileId)
    {
        var delay = await _userSettingsProvider.GetOperationDelayAsync();
        await Task.Delay(delay);
        
        var response = await _client.GetAsync($"/files/download/{fileId}");
        response.EnsureSuccessStatusCode();
        var fileBytes = await response.Content.ReadAsByteArrayAsync();

        return fileBytes;
        
    }

    public async Task DeleteFileAsync(int fileId)
    {
        var delay = await _userSettingsProvider.GetOperationDelayAsync();
        await Task.Delay(delay);
        
        var response = await _client.DeleteAsync($"/files/{fileId}");
        response.EnsureSuccessStatusCode();
    }
}