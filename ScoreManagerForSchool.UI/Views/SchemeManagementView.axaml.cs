using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ScoreManagerForSchool.UI.ViewModels;
using System;
using System.IO;
using Avalonia;
using ScoreManagerForSchool.UI.Services;

namespace ScoreManagerForSchool.UI.Views
{
    public partial class SchemeManagementView : UserControl
    {
        public SchemeManagementView()
        {
            InitializeComponent();
            if (DataContext == null) DataContext = new SchemeManagementViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void OnImportClasses(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var tb = this.FindControl<TextBox>("ClassesCsvBox");
            var cb = this.FindControl<CheckBox>("ClassesHeaderCheck");
            var path = tb?.Text ?? string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(path)) { await ShowMessageAsync("提示", "请先选择 CSV/Excel 文件。"); return; }
                if (!File.Exists(path)) { await ShowMessageAsync("错误", "文件不存在：" + path); return; }
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext != ".csv" && ext != ".xls" && ext != ".xlsx")
                {
                    await ShowMessageAsync("错误", "仅支持 CSV/XLS/XLSX 文件。");
                    return;
                }
                if (DataContext is SchemeManagementViewModel vm)
                    vm.ImportClasses(path, cb?.IsChecked ?? true);
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("导入失败", ex.Message);
            }
        }

        private async void OnImportSchemes(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var tb = this.FindControl<TextBox>("SchemesCsvBox");
            var cb = this.FindControl<CheckBox>("SchemesHeaderCheck");
            var path = tb?.Text ?? string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(path)) { await ShowMessageAsync("提示", "请先选择 CSV/Excel 文件。"); return; }
                if (!File.Exists(path)) { await ShowMessageAsync("错误", "文件不存在：" + path); return; }
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext != ".csv" && ext != ".xls" && ext != ".xlsx")
                {
                    await ShowMessageAsync("错误", "仅支持 CSV/XLS/XLSX 文件。");
                    return;
                }
                if (DataContext is SchemeManagementViewModel vm)
                    vm.ImportSchemes(path, cb?.IsChecked ?? true);
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("导入失败", ex.Message);
            }
        }

        private async void OnBrowseClasses(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null)
                {
                    var path = await FilePickerUtil.PickCsvOrExcelToLocalPathAsync(topLevel, "选择 CSV/Excel 文件");
                    if (!string.IsNullOrEmpty(path))
                    {
                        var tb = this.FindControl<TextBox>("ClassesCsvBox");
                        if (tb != null) tb.Text = path;
                    }
                }
            }
            catch { }
        }

        private async void OnBrowseSchemes(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null)
                {
                    var path = await FilePickerUtil.PickCsvOrExcelToLocalPathAsync(topLevel, "选择 CSV/Excel 文件");
                    if (!string.IsNullOrEmpty(path))
                    {
                        var tb = this.FindControl<TextBox>("SchemesCsvBox");
                        if (tb != null) tb.Text = path;
                    }
                }
            }
            catch { }
        }

        private async System.Threading.Tasks.Task ShowMessageAsync(string title, string message)
        {
            try
            {
                var panel = new StackPanel { Margin = new Thickness(12), Spacing = 8 };
                panel.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
                var ok = new Button { Content = "确定", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
                panel.Children.Add(ok);
                var dlg = new Window { Title = title, Width = 420, Height = 160, Content = panel, WindowStartupLocation = WindowStartupLocation.CenterOwner };
                var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                ok.Click += (_, __) => { try { dlg.Close(); } catch { } tcs.TrySetResult(true); };
                if (this.VisualRoot is Window owner)
                    await dlg.ShowDialog(owner);
                else
                {
                    dlg.Show();
                    await tcs.Task.ConfigureAwait(false);
                    try { dlg.Close(); } catch { }
                }
            }
            catch { }
        }

        private async void OnShowFormatInfo(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var msg = "支持的文件: CSV、XLS、XLSX\n\n" +
                      "班级列表列顺序: 班级, 类型\n" +
                      "评价方案: 任意列，自由格式（可选表头）\n\n" +
                      "注意:\n- 勾选‘首行为表头’时将跳过第一行\n- CSV 请使用逗号分隔\n- Excel 多表会顺序读取所有工作表\n- 仅限扩展名 .csv/.xls/.xlsx";
            await ShowMessageAsync("导入格式说明", msg);
        }
    }
}
