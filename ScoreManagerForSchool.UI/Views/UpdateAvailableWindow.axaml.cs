using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Diagnostics;

namespace ScoreManagerForSchool.UI.Views
{
    public partial class UpdateAvailableWindow : Window
    {
        public UpdateAvailableWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnClose(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnUpdateNow(object? sender, RoutedEventArgs e)
        {
            try
            {
                ScoreManagerForSchool.UI.Services.Updater.StartUpdaterU(AppContext.BaseDirectory);
                Close();
            }
            catch { }
        }
    }
}
