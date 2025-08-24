using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ScoreManagerForSchool.UI.ViewModels;
using System.Linq;
using System;
using System.IO;
using ScoreManagerForSchool.Core.Storage;
using Avalonia;
using Avalonia.Controls.Primitives;
using ScoreManagerForSchool.UI.Services;

namespace ScoreManagerForSchool.UI.Views
{
    public partial class StudentsListView : UserControl
    {
        public StudentsListView()
        {
            InitializeComponent();
            if (DataContext == null)
                DataContext = new StudentsViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void OnBrowseCsv(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(this);
                if (topLevel != null)
                {
                    var path = await FilePickerUtil.PickCsvOrExcelToLocalPathAsync(topLevel, "选择 CSV/Excel 文件");
                    if (!string.IsNullOrEmpty(path))
                    {
                        TextBox? tb = this.FindControl<TextBox>("CsvPathBox");
                        if (tb != null && !string.IsNullOrEmpty(path)) tb.Text = path;
                    }
                }
            }
            catch { }
        }

    private async void OnPreviewImport(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                var tb = this.FindControl<TextBox>("CsvPathBox");
                var chk = this.FindControl<CheckBox>("HeaderCheck");
                var path = tb?.Text ?? string.Empty;
                if (string.IsNullOrWhiteSpace(path)) { await ShowMessageAsync("提示", "请先选择 CSV/Excel 文件。"); return; }
                if (!File.Exists(path)) { await ShowMessageAsync("错误", "文件不存在：" + path); return; }
                var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
                if (ext != ".csv" && ext != ".xls" && ext != ".xlsx")
                {
                    await ShowMessageAsync("错误", "仅支持 CSV/XLS/XLSX 文件。");
                    return;
                }
                if (DataContext is StudentsViewModel vm)
                    vm.ImportCsv(path, chk?.IsChecked ?? true);
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("预览失败", ex.Message);
            }
        }

        private async void OnBulkChangeClass(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var baseDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "base");
            if (!ScoreManagerForSchool.UI.Security.AuthManager.Ensure(baseDir)) return;
            // Dialog: show a textbox for CSV (Id,NewClass), a checkbox for reset scores, and buttons
            var dlg = new Window { Title = "批量更改班级", Width = 560, Height = 380 };
            var csvBox = new TextBox { AcceptsReturn = true, Watermark = "csv: 唯一号,新班级" };
            csvBox.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
            var chk = new CheckBox { Content = "清零分数", IsChecked = false };
            var importBtn = new Button { Content = "导入文件..." };
            var ok = new Button { Content = "确定" };
            var cancel = new Button { Content = "取消" };
            var panel = new StackPanel { Margin = new Thickness(12), Spacing = 8 };
            panel.Children.Add(importBtn);
            panel.Children.Add(csvBox);
            panel.Children.Add(chk);
            var btns = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Spacing = 8 };
            btns.Children.Add(cancel); btns.Children.Add(ok);
            panel.Children.Add(btns);
            dlg.Content = panel;

            importBtn.Click += async (_, __) =>
            {
                if (this.VisualRoot is Window owner)
                {
                    var provider = owner.StorageProvider;
                    var res = await provider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions { AllowMultiple = false, Title = "选择 CSV 文件" });
                    if (res != null && res.Count > 0)
                    {
                        try
                        {
                            var file = res[0];
                            await using var stream = await file.OpenReadAsync();
                            using var sr = new StreamReader(stream);
                            csvBox.Text = await sr.ReadToEndAsync();
                        }
                        catch { }
                    }
                }
            };

            ok.Click += (_, __) => dlg.Close(true);
            cancel.Click += (_, __) => dlg.Close(false);

            bool confirmed = false;
            if (this.VisualRoot is Window win)
                confirmed = await dlg.ShowDialog<bool>(win);
            else
            {
                var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                dlg.Closed += (_, __) => tcs.TrySetResult(false);
                dlg.Show();
                await tcs.Task;
            }
            if (!confirmed) return;

            // Parse CSV text
            var text = csvBox.Text ?? string.Empty;
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var pairs = lines.Select(l => l.Split(',')).Where(p => p.Length >= 2).Select(p => (Id: p[0].Trim(), NewClass: p[1].Trim())).ToList();
            if (pairs.Count == 0) return;

            var sstore = new StudentStore(baseDir);
            var estore = new EvaluationStore(baseDir);
            var students = sstore.Load();
            foreach (var (Id, NewClass) in pairs)
            {
                var st = students.FirstOrDefault(s => string.Equals(s.Id, Id, StringComparison.OrdinalIgnoreCase));
                if (st != null) st.Class = NewClass;
            }
            sstore.Save(students);

            if (chk.IsChecked == true)
            {
                // reset scores for affected students
                var evals = estore.Load();
                evals.RemoveAll(e => pairs.Any(p => string.Equals(p.Id, e.StudentId, StringComparison.OrdinalIgnoreCase)));
                estore.Save(evals);
            }

            if (DataContext is StudentsViewModel vm)
            {
                vm.Load();
            }
        }

        private async void OnImportAndSave(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                var tb = this.FindControl<TextBox>("CsvPathBox");
                var cb = this.FindControl<CheckBox>("HeaderCheck");
                var path = tb?.Text ?? string.Empty;
                if (string.IsNullOrWhiteSpace(path)) { await ShowMessageAsync("提示", "请先选择 CSV/Excel 文件。"); return; }
                if (!File.Exists(path)) { await ShowMessageAsync("错误", "文件不存在：" + path); return; }
                var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
                if (ext != ".csv" && ext != ".xls" && ext != ".xlsx")
                {
                    await ShowMessageAsync("错误", "仅支持 CSV/XLS/XLSX 文件。");
                    return;
                }

                var header = cb?.IsChecked ?? true;
                var imported = CsvImporter.ImportStudents(path, header);
                var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "base");
                if (!ScoreManagerForSchool.UI.Security.AuthManager.Ensure(baseDir)) return;
                new StudentStore(baseDir).Save(imported);
                if (DataContext is StudentsViewModel vm)
                {
                    vm.Load();
                }
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("导入失败", ex.Message);
            }
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
                      "学生名单列顺序: 班级, 唯一号, 姓名\n" +
                      "班级列表列顺序: 班级, 类型\n" +
                      "评价方案: 任意列，自由格式（可选表头）\n\n" +
                      "注意:\n- 勾选‘首行为表头’时将跳过第一行\n- CSV 请使用逗号分隔\n- Excel 多表会顺序读取所有工作表\n- 仅限扩展名 .csv/.xls/.xlsx";
            await ShowMessageAsync("导入格式说明", msg);
        }
    }
}
