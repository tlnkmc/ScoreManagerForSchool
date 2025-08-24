using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ScoreManagerForSchool.UI.ViewModels;
using ScoreManagerForSchool.Core.Storage;
using Avalonia;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ScoreManagerForSchool.UI.Views
{
    public partial class StatsView : UserControl
    {
        public StatsView()
        {
            InitializeComponent();
            if (DataContext == null)
            {
                var vm = new StatsViewModel();
                vm.ShowExportDialog = ShowExportDialogAsync;
                DataContext = vm;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async Task<(bool confirmed, DateTime startDate, DateTime endDate)> ShowExportDialogAsync()
        {
            var dialog = new ExportScoreDialog();
            
            if (this.VisualRoot is Window owner)
            {
                await dialog.ShowDialog(owner);
            }
            else
            {
                dialog.Show();
                // 等待窗口关闭
                var tcs = new TaskCompletionSource<bool>();
                dialog.Closed += (_, __) => tcs.TrySetResult(true);
                await tcs.Task;
            }

            return (dialog.IsConfirmed, dialog.ViewModel.StartDate, dialog.ViewModel.EndDate);
        }

        private async void OnProcessPendingClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not StatsViewModel vm) return;
            // determine bound item by traversing visual tree (simplified approach)
            if (sender is Button btn)
            {
                if (btn.DataContext is EvaluationEntry entry)
                {
                    var dlg = new Window { Title = "处理待处理项", Width = 420, Height = 220 };
                    var nameBox = new TextBox { Watermark = "姓名", Text = entry.Name ?? string.Empty };
                    var classBox = new TextBox { Watermark = "班级", Text = entry.Class ?? string.Empty };
                    var ok = new Button { Content = "确定" };
                    var cancel = new Button { Content = "取消" };
                    var grid = new Grid { RowDefinitions = new RowDefinitions("*,*,Auto"), Margin = new Thickness(12) };
                    grid.Children.Add(nameBox);
                    Grid.SetRow(classBox, 1); grid.Children.Add(classBox);
                    var panel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Spacing = 8 };
                    panel.Children.Add(cancel); panel.Children.Add(ok); Grid.SetRow(panel, 2); grid.Children.Add(panel);
                    dlg.Content = grid;

                    ok.Click += (_, __) => dlg.Close(true);
                    cancel.Click += (_, __) => dlg.Close(false);
                    bool result = false;
                    if (this.VisualRoot is Window owner)
                        result = (await dlg.ShowDialog<bool>(owner));
                    else
                    {
                        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                        dlg.Closed += (_, __) => tcs.TrySetResult(false);
                        dlg.Show();
                        await tcs.Task;
                    }
                    if (result)
                    {
                        entry.Name = nameBox.Text;
                        entry.Class = classBox.Text;
                        // reuse existing command to persist and reload
                        vm.ProcessPendingCommand.Execute(entry);
                    }
                }
            }
        }

        private void OnDeletePendingClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not StatsViewModel vm) return;
            if (sender is Button btn && btn.DataContext is EvaluationEntry entry)
            {
                vm.DeletePendingCommand.Execute(entry);
            }
        }

        private async void OnEditSummaryClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not StatsViewModel vm) return;
            if (sender is Button btn && btn.DataContext is StatsSummaryItem item)
            {
                var dlg = new Window { Title = "编辑信息", Width = 440, Height = 240 };
                var classBox = new TextBox { Watermark = "班级", Text = item.Class ?? string.Empty };
                var idBox = new TextBox { Watermark = "唯一号", Text = item.Id ?? string.Empty };
                var nameBox = new TextBox { Watermark = "姓名", Text = item.Name ?? string.Empty };
                var ok = new Button { Content = "保存" };
                var cancel = new Button { Content = "取消" };
                var grid = new Grid { RowDefinitions = new RowDefinitions("*,*,*,Auto"), Margin = new Thickness(12) };
                grid.Children.Add(classBox);
                Grid.SetRow(idBox, 1); grid.Children.Add(idBox);
                Grid.SetRow(nameBox, 2); grid.Children.Add(nameBox);
                var buttons = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Spacing = 8 };
                buttons.Children.Add(cancel); buttons.Children.Add(ok); Grid.SetRow(buttons, 3); grid.Children.Add(buttons);
                dlg.Content = grid;
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

                var baseDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "base");
                if (!ScoreManagerForSchool.UI.Security.AuthManager.Ensure(baseDir)) return;
                var sstore = new StudentStore(baseDir);
                var students = sstore.Load();
                var st = students.FirstOrDefault(s => s.Id == item.Id) ?? new Student();
                st.Class = classBox.Text; st.Id = idBox.Text; st.Name = nameBox.Text;
                if (!students.Any(s => s.Id == st.Id)) students.Add(st);
                sstore.Save(students);
                vm.RefreshCommand.Execute(null);
            }
        }

        private void OnDeleteSummaryClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not StatsViewModel vm) return;
            if (sender is Button btn && btn.DataContext is StatsSummaryItem item)
            {
                var baseDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "base");
                if (!ScoreManagerForSchool.UI.Security.AuthManager.Ensure(baseDir)) return;
                var estore = new EvaluationStore(baseDir);
                var evals = estore.Load();
                // delete all records matching this person (by Id first, else by Class+Name)
                if (!string.IsNullOrWhiteSpace(item.Id))
                    evals.RemoveAll(e => string.Equals(e.StudentId, item.Id, StringComparison.OrdinalIgnoreCase));
                else
                    evals.RemoveAll(e => string.Equals(e.Class, item.Class, StringComparison.OrdinalIgnoreCase) && string.Equals(e.Name, item.Name, StringComparison.OrdinalIgnoreCase));
                estore.Save(evals);
                vm.RefreshCommand.Execute(null);
            }
        }
    }
}
