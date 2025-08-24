using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ScoreManagerForSchool.Core.Storage;
using ScoreManagerForSchool.UI.ViewModels;

namespace ScoreManagerForSchool.UI.Views;

public partial class InfoEntryView : UserControl
{
    public InfoEntryView()
    {
        InitializeComponent();
        if (DataContext == null)
        {
            var vm = new InfoEntryViewModel();
            vm.SelectStudentAsync = ShowSelectDialogAsync;
            vm.SelectMultiAsync = ShowMultiSelectDialogAsync;
            DataContext = vm;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async Task<Student?> ShowSelectDialogAsync(List<Student> candidates, string line)
    {
        var win = new Window
        {
            Width = 480,
            Height = 360,
            Title = "选择学生",
        };
    var list = new ListBox { ItemsSource = candidates.Select(c => $"{c.Name} - {c.Class} - {c.Id}").ToList() };
        var ok = new Button { Content = "确定" };
        var cancel = new Button { Content = "取消" };
        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            Margin = new Avalonia.Thickness(12)
        };
        grid.Children.Add(new TextBlock { Text = line, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        Grid.SetRow(list, 1);
        grid.Children.Add(list);
        var panel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Spacing = 8 };
        panel.Children.Add(cancel);
        panel.Children.Add(ok);
        Grid.SetRow(panel, 2);
        grid.Children.Add(panel);
        win.Content = grid;

        Student? chosen = null;
        ok.Click += (_, __) =>
        {
            var idx = list.SelectedIndex;
            if (idx >= 0 && idx < candidates.Count)
                chosen = candidates[idx];
            win.Close();
        };
        cancel.Click += (_, __) => win.Close();
        var owner = this.VisualRoot as Window;
        if (owner != null)
            await win.ShowDialog(owner);
        else
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
            win.Closed += (_, __) => tcs.TrySetResult(true);
            win.Show();
            await tcs.Task;
        }
        return chosen;
    }

    private async Task<List<Student>?> ShowMultiSelectDialogAsync(List<Student> candidates, string line)
    {
        var win = new Window
        {
            Width = 520,
            Height = 420,
            Title = "选择学生（可多选）",
        };
        var list = new ListBox { SelectionMode = SelectionMode.Multiple, ItemsSource = candidates.Select(c => $"{c.Name} - {c.Class} - {c.Id}").ToList() };
        var ok = new Button { Content = "确定" };
        var cancel = new Button { Content = "取消" };
        var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), Margin = new Avalonia.Thickness(12) };
        grid.Children.Add(new TextBlock { Text = line, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        Grid.SetRow(list, 1);
        grid.Children.Add(list);
        var panel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Spacing = 8 };
        panel.Children.Add(cancel);
        panel.Children.Add(ok);
        Grid.SetRow(panel, 2);
        grid.Children.Add(panel);
        win.Content = grid;

        List<Student>? chosen = null;
        ok.Click += (_, __) =>
        {
            var idxs = list.SelectedItems?.Cast<object?>()?.Select(o => list.Items!.IndexOf(o)).Where(i => i >= 0).Distinct().ToList() ?? new List<int>();
            if (idxs.Count > 0)
            {
                chosen = idxs.Where(i => i < candidates.Count).Select(i => candidates[i]).ToList();
            }
            win.Close();
        };
        cancel.Click += (_, __) => win.Close();
        var owner = this.VisualRoot as Window;
        if (owner != null)
            await win.ShowDialog(owner);
        else
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
            win.Closed += (_, __) => tcs.TrySetResult(true);
            win.Show();
            await tcs.Task;
        }
        return chosen;
    }
}
