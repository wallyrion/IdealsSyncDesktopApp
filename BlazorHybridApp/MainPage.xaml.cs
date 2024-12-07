using BlazorHybridApp.Core;
using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;

namespace BlazorHybridApp
{
    public partial class MainPage
    {
        private bool IsWindowVisible { get; set; } = true;
        public MainPage()
        {
            InitializeComponent();
            
            BindingContext = this;

            AppGlobalState.AppWasClosed += () =>
            {
                ShowHideWindow();
            };
        }
        
        
        [RelayCommand]
        public void ShowHideWindow()
        {
            var window = Application.Current?.Windows[0];
            if (window == null)
            {
                return;
            }

            if (IsWindowVisible)
            {
                window.Hide();
            }
            else
            {
                window.Show();
            }
            IsWindowVisible = !IsWindowVisible;
        }
        
        
        [RelayCommand]
        public void ExitApplication()
        {
            Application.Current?.Quit();
        }

    }
    
    
    
}
