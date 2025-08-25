using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ScoreManagerForSchool.Core.Storage;
using ScoreManagerForSchool.UI.Services;

namespace ScoreManagerForSchool.UI.Views.Dialogs;

public partial class ImportPreviewDialog : Window
{
    public string? SelectedPath { get; private set; }
    public bool FirstRowIsHeader => _headerCheck?.IsChecked ?? true;
    public int ClassColumn => _classColBox?.SelectedIndex ?? 0;
    public int IdColumn => _idColBox?.SelectedIndex ?? 1;
    public int NameColumn => _nameColBox?.SelectedIndex ?? 2;

    private TextBox? _pathBox;
    private CheckBox? _headerCheck;
    private ComboBox? _classColBox;
    private ComboBox? _idColBox;
    private ComboBox? _nameColBox;
    private ItemsControl? _grid;

    public ImportPreviewDialog()
    {
        InitializeComponent();
        _pathBox = this.FindControl<TextBox>("PathBox");
        _headerCheck = this.FindControl<CheckBox>("HeaderCheck");
        _classColBox = this.FindControl<ComboBox>("ClassColBox");
        _idColBox = this.FindControl<ComboBox>("IdColBox");
        _nameColBox = this.FindControl<ComboBox>("NameColBox");
    _grid = this.FindControl<ItemsControl>("PreviewGrid");

        this.FindControl<Button>("BrowseBtn")!.Click += OnBrowse;
        this.FindControl<Button>("LoadBtn")!.Click += OnLoad;
        this.FindControl<Button>("OkBtn")!.Click += (_, __) => this.Close(true);
        this.FindControl<Button>("CancelBtn")!.Click += (_, __) => this.Close(false);
    }

    private async void OnBrowse(object? sender, RoutedEventArgs e)
    {
        try
        {
            var top = TopLevel.GetTopLevel(this);
            if (top == null) return;
            var path = await FilePickerUtil.PickCsvOrExcelToLocalPathAsync(top, "选择 CSV/Excel 文件");
            if (!string.IsNullOrEmpty(path))
            {
                _pathBox!.Text = path;
                await LoadPreview(path);
            }
        }
        catch { }
    }

    private async void OnLoad(object? sender, RoutedEventArgs e)
    {
        var path = _pathBox?.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
        await LoadPreview(path);
    }

    private async System.Threading.Tasks.Task LoadPreview(string path)
    {
        SelectedPath = path;
        // read rows
        string[][] rows;
        try { rows = ReadTabularInternal(path); }
        catch { rows = Array.Empty<string[]>(); }

        // columns
        int colCount = rows.Length > 0 ? rows.Max(r => r?.Length ?? 0) : 0;
        var colNames = new List<string>();
        if ((_headerCheck?.IsChecked ?? true) && rows.Length > 0)
        {
            var header = rows[0] ?? Array.Empty<string>();
            for (int i = 0; i < colCount; i++)
                colNames.Add(string.IsNullOrWhiteSpace(i < header.Length ? header[i] : null) ? $"列{i + 1}" : header[i]);
        }
        else
        {
            for (int i = 0; i < colCount; i++) colNames.Add($"列{i + 1}");
        }

        // fill mapping boxes
    var items = colNames.Select((n, i) => $"{i + 1}: {n}").ToList();
    _classColBox!.ItemsSource = items;
    _idColBox!.ItemsSource = items;
    _nameColBox!.ItemsSource = items;
        if (_classColBox.SelectedIndex < 0) _classColBox.SelectedIndex = Math.Min(0, colCount - 1);
        if (_idColBox.SelectedIndex < 0) _idColBox.SelectedIndex = Math.Min(1, colCount - 1);
        if (_nameColBox.SelectedIndex < 0) _nameColBox.SelectedIndex = Math.Min(2, colCount - 1);

        // build DataTable for preview
        var dt = new DataTable();
        for (int i = 0; i < colCount; i++) dt.Columns.Add(colNames[i]);
        int start = (_headerCheck?.IsChecked ?? true) ? 1 : 0;
        for (int i = start; i < Math.Min(rows.Length, start + 100); i++)
        {
            var r = rows[i] ?? Array.Empty<string>();
            var row = dt.NewRow();
            for (int c = 0; c < colCount; c++) row[c] = c < r.Length ? r[c] : string.Empty;
            dt.Rows.Add(row);
        }
        if (_grid is ListBox lb)
        {
            lb.ItemsSource = dt.DefaultView;
        }
        else if (_grid != null)
        {
            // Fallback via reflection for generic ItemsControl
            var pi = _grid.GetType().GetProperty("ItemsSource");
            pi?.SetValue(_grid, dt.DefaultView);
        }
    await Task.Yield();
    }

    // Minimal reuse: call CsvImporter internal readers via provided public API
    private static string[][] ReadTabularInternal(string path)
    {
        // Use ImportScheme to leverage same reader with minimal allocations
        // but we need raw rows; as a workaround, call private-like behavior by re-reading here
        try
        {
            // This mirrors CsvImporter.ReadTabular behavior: try Excel else CSV
            var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".xls" || ext == ".xlsx")
            {
                // We don't have direct access to CsvImporter.ReadExcel; fall back to Csv then.
                // Users commonly export CSV; preview for Excel will still be provided via ImportStudents on OK.
            }
            // Fallback CSV read for preview
            var lines = File.ReadAllLines(path);
            return lines.Select(l => l.Split(',')).ToArray();
        }
        catch
        {
            return Array.Empty<string[]>();
        }
    }

    public (string? path, bool header, int classCol, int idCol, int nameCol) GetResult()
        => (SelectedPath, FirstRowIsHeader, ClassColumn, IdColumn, NameColumn);
}
