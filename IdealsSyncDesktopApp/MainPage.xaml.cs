using BlazorHybridApp.Core;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;

namespace IdealsSyncDesktopApp
{
    public partial class MainPage : ContentPage
    {
        private bool IsWindowVisible { get; set; } = true;

        public MainPage()
        {
            InitializeComponent();

            BindingContext = this;

            GlobalEvents.AppWasClosed += () => { ShowHideWindow(); };
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