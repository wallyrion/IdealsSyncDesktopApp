using System.Threading.Tasks;

namespace BlazorHybridApp.Core
{
    internal class BackgroundTest(FileSyncService fileSyncService)
    {
        public async Task StartAsync()
        {
            while (true)
            {
                await Task.Delay(1000);
                await fileSyncService.SyncAsync();
            }
        }
    }
}
