using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ScoreManagerForSchool.Core.Storage;
using ScoreManagerForSchool.UI.ViewModels;
using ScoreManagerForSchool.UI.Controls;
using Avalonia;
using System;

namespace ScoreManagerForSchool.UI.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            if (this.DataContext is not SettingsViewModel)
                this.DataContext = new SettingsViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void OnResetClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Ask for password to confirm
            var dlg = new Window { Title = "确认重置", Width = 420, Height = 200 };
            var info = new TextBlock { Text = "输入当前用户密码以确认重置：", Margin = new Thickness(0,0,0,8) };
            var pwd = new PasswordTextBox();
            var ok = new Button { Content = "确定" };
            var cancel = new Button { Content = "取消" };
            var panel = new StackPanel { Margin = new Thickness(12), Spacing = 8 };
            panel.Children.Add(info); panel.Children.Add(pwd);
            var btns = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Spacing = 8 };
            btns.Children.Add(cancel); btns.Children.Add(ok); panel.Children.Add(btns);
            dlg.Content = panel;
            ok.Click += (_, __) => dlg.Close(true);
            cancel.Click += (_, __) => dlg.Close(false);
            bool result = false;
            if (this.VisualRoot is Window owner)
                result = await dlg.ShowDialog<bool>(owner);
            else
            {
                var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                dlg.Closed += (_, __) => tcs.TrySetResult(false);
                dlg.Show();
                await tcs.Task;
            }
            if (!result) return;

            // verify password
            var baseDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "base");
            try
            {
                var db = new ScoreManagerForSchool.Core.Storage.Database1Store(baseDir).Load();
                if (db == null || string.IsNullOrEmpty(db.Salt1) || string.IsNullOrEmpty(db.ID1)) return;
                var salt1 = Convert.FromBase64String(db.Salt1);
                var key1 = ScoreManagerForSchool.Core.Security.CryptoUtil.DeriveKey(pwd.Text ?? string.Empty, salt1, 32, db.Iterations > 0 ? db.Iterations : 100000);
                var plain = ScoreManagerForSchool.Core.Security.CryptoUtil.DecryptFromBase64(db.ID1, key1);
                if (string.IsNullOrEmpty(plain) || !plain.StartsWith("0D0007211145141919810", StringComparison.Ordinal)) return;
            }
            catch { return; }

            // delete files
            ScoreManagerForSchool.Core.Storage.Database1Store.DeleteBaseFiles(baseDir);
            if (this.DataContext is SettingsViewModel vm) vm.GetType().GetMethod("Save", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(vm, null);
        }
    }
}
