using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Microsoft.Maui;

namespace BlazorHybridApp
{
    public class WindowsFolderPickerService : IFolderPickerService
    {
        public async Task<string> PickFolderAsync()
        {
            var folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };

            // Set XAML Window handle
            var hwnd = ((MauiWinUIWindow)App.Current.Windows[0].Handler.PlatformView).WindowHandle;
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            return folder?.Path ?? string.Empty;
        }
    }
}
