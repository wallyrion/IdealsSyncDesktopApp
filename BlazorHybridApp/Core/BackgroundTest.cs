using System.Threading.Tasks;

namespace BlazorHybridApp.Core
{
    internal class BackgroundTest(FileSyncService fileSyncService)
    {
        private bool _isStarted;
        
        public async Task StartAsync()
        {
            if (_isStarted)
            {
                return;
            }
            
            _isStarted = true;
            while (true)
            {
                await Task.Delay(10000);

                try
                {
                    await fileSyncService.SyncAsync();

                }
                catch (Exception e)
                {
                }
            }
        }
    }
}
