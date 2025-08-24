using Avalonia.Controls;
using Avalonia.Interactivity;
using ScoreManagerForSchool.UI.ViewModels;
using System.Collections.Generic;
using ScoreManagerForSchool.Core.Storage;

namespace ScoreManagerForSchool.UI.Views;

public partial class StudentsView : UserControl
{
    public StudentsView()
    {
        InitializeComponent();
    }

    private void OnPreviewImport(object? sender, RoutedEventArgs e)
    {
        var pathBox = this.FindControl<TextBox>("CsvPathBox");
        var path = pathBox?.Text;
        if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return;

        var imported = CsvImporter.ImportStudents(path);
        var panel = new StackPanel();
        foreach (var s in imported)
        {
            var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
            row.Children.Add(new TextBlock { Text = s.Class, Width = 120 });
            row.Children.Add(new TextBlock { Text = s.Id, Width = 120 });
            row.Children.Add(new TextBlock { Text = s.Name });
            panel.Children.Add(row);
        }
        var applyButton = new Button { Content = "应用导入", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Margin = new Avalonia.Thickness(8) };
        var root = new StackPanel();
        root.Children.Add(new ScrollViewer { Content = panel, Height = 320 });
        root.Children.Add(applyButton);

        var win = new Window
        {
            Title = "导入预览",
            Width = 600,
            Height = 420,
            Content = root
        };

        applyButton.Click += (_, __) =>
        {
            if (this.DataContext is StudentsViewModel vm)
            {
                vm.ApplyImported(new List<Student>(imported));
            }
            win.Close();
        };

        // determine owner window safely
        Window? owner = null;
        var app = Avalonia.Application.Current;
        if (app != null && app.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime lt)
            owner = lt.MainWindow;
        if (owner != null)
            win.ShowDialog(owner);
        else
            win.Show();
    }
}
