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
    public int FirstDataRow => _firstDataRowBox?.SelectedIndex + 1 ?? 1;

    private readonly string _baseDir;
    private TextBlock? _pathDisplay;
    private CheckBox? _headerCheck;
    private ComboBox? _classColBox;
    private ComboBox? _idColBox;
    private ComboBox? _nameColBox;
    private ComboBox? _firstDataRowBox;
    private DataGrid? _grid;
    private string[][]? _allRows;

    // 默认构造函数（XAML要求）
    public ImportPreviewDialog() : this(Path.Combine(Directory.GetCurrentDirectory(), "base"))
    {
    }

    public ImportPreviewDialog(string baseDir, string? initialPath = null)
    {
        _baseDir = baseDir;
        InitializeComponent();
        InitializeControls();
        
        if (!string.IsNullOrEmpty(initialPath) && File.Exists(initialPath))
        {
            _ = LoadFileAsync(initialPath);
        }
    }

    private void InitializeControls()
    {
        _pathDisplay = this.FindControl<TextBlock>("PathDisplay");
        _headerCheck = this.FindControl<CheckBox>("HeaderCheck");
        _classColBox = this.FindControl<ComboBox>("ClassColBox");
        _idColBox = this.FindControl<ComboBox>("IdColBox");
        _nameColBox = this.FindControl<ComboBox>("NameColBox");
        _firstDataRowBox = this.FindControl<ComboBox>("FirstDataRowBox");
        _grid = this.FindControl<DataGrid>("PreviewGrid");

        // 事件绑定
        this.FindControl<Button>("ImportBtn")!.Click += OnImport;
        this.FindControl<Button>("CancelBtn")!.Click += (_, __) => this.Close(false);
        
        // 当选择发生变化时自动更新预览
        if (_headerCheck != null) _headerCheck.PropertyChanged += (_, __) => UpdatePreview();
        if (_classColBox != null) _classColBox.SelectionChanged += (_, __) => UpdatePreview();
        if (_idColBox != null) _idColBox.SelectionChanged += (_, __) => UpdatePreview();
        if (_nameColBox != null) _nameColBox.SelectionChanged += (_, __) => UpdatePreview();
        if (_firstDataRowBox != null) _firstDataRowBox.SelectionChanged += (_, __) => UpdatePreview();
    }

    public async Task LoadFileAsync(string path)
    {
        try
        {
            SelectedPath = path;
            if (_pathDisplay != null)
            {
                _pathDisplay.Text = Path.GetFileName(path);
                _pathDisplay.Foreground = Avalonia.Media.Brushes.Black;
            }

            // 读取文件数据
            _allRows = await Task.Run(() => ReadTabularInternal(path));
            
            // 初始化列选择器
            await InitializeColumnSelectors();
            
            // 更新预览
            UpdatePreview();
        }
        catch (Exception ex)
        {
            if (_pathDisplay != null)
            {
                _pathDisplay.Text = $"加载失败: {ex.Message}";
                _pathDisplay.Foreground = Avalonia.Media.Brushes.Red;
            }
        }
    }

    private async Task InitializeColumnSelectors()
    {
        if (_allRows == null || _allRows.Length == 0) return;

        int colCount = _allRows.Max(r => r?.Length ?? 0);
        var colNames = new List<string>();
        
        // 使用第一行作为列名（如果有表头）或生成默认列名
        if (_allRows.Length > 0)
        {
            var firstRow = _allRows[0] ?? Array.Empty<string>();
            for (int i = 0; i < colCount; i++)
            {
                string colName = i < firstRow.Length && !string.IsNullOrWhiteSpace(firstRow[i]) 
                    ? firstRow[i] 
                    : $"列{i + 1}";
                colNames.Add($"{i + 1}: {colName}");
            }
        }

        // 填充列选择器
        var items = colNames.ToArray();
        if (_classColBox != null) _classColBox.ItemsSource = items;
        if (_idColBox != null) _idColBox.ItemsSource = items;
        if (_nameColBox != null) _nameColBox.ItemsSource = items;

        // 设置默认选择
        if (_classColBox != null && _classColBox.SelectedIndex < 0) _classColBox.SelectedIndex = Math.Min(0, colCount - 1);
        if (_idColBox != null && _idColBox.SelectedIndex < 0) _idColBox.SelectedIndex = Math.Min(1, colCount - 1);
        if (_nameColBox != null && _nameColBox.SelectedIndex < 0) _nameColBox.SelectedIndex = Math.Min(2, colCount - 1);

        // 初始化数据行选择器（支持多行表头）
        var rowOptions = new List<string>();
        for (int i = 1; i <= Math.Min(10, _allRows.Length); i++)
        {
            rowOptions.Add($"第 {i} 行");
        }
        if (_firstDataRowBox != null)
        {
            _firstDataRowBox.ItemsSource = rowOptions.ToArray();
            _firstDataRowBox.SelectedIndex = 0; // 默认第1行
        }

        await Task.Yield();
    }

    private void UpdatePreview()
    {
        if (_allRows == null || _grid == null) return;

        try
        {
            int colCount = _allRows.Max(r => r?.Length ?? 0);
            var dt = new DataTable();

            // 创建列
            var colNames = new List<string>();
            if (FirstRowIsHeader && _allRows.Length > 0)
            {
                var header = _allRows[0] ?? Array.Empty<string>();
                for (int i = 0; i < colCount; i++)
                {
                    string colName = i < header.Length && !string.IsNullOrWhiteSpace(header[i]) 
                        ? header[i] 
                        : $"列{i + 1}";
                    colNames.Add(colName);
                    dt.Columns.Add(colName);
                }
            }
            else
            {
                for (int i = 0; i < colCount; i++)
                {
                    colNames.Add($"列{i + 1}");
                    dt.Columns.Add($"列{i + 1}");
                }
            }

            // 添加数据行
            int startRow = FirstDataRow - 1; // 转换为0基索引
            int previewLimit = Math.Min(_allRows.Length, startRow + 50); // 预览最多50行
            
            for (int i = startRow; i < previewLimit; i++)
            {
                if (i < 0 || i >= _allRows.Length) continue;
                
                var sourceRow = _allRows[i] ?? Array.Empty<string>();
                var row = dt.NewRow();
                
                for (int c = 0; c < colCount; c++)
                {
                    row[c] = c < sourceRow.Length ? (sourceRow[c] ?? string.Empty) : string.Empty;
                }
                
                dt.Rows.Add(row);
            }

            _grid.ItemsSource = dt.DefaultView;
        }
        catch
        {
            // 预览失败时清空
            _grid.ItemsSource = null;
        }
    }

    private async void OnImport(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(SelectedPath) || !File.Exists(SelectedPath)) 
        {
            await ShowMessageAsync("错误", "请先选择有效的文件。");
            return;
        }

        try
        {
            // 执行导入
            var students = CsvImporter.ImportStudents(
                SelectedPath, 
                FirstRowIsHeader, 
                ClassColumn, 
                IdColumn, 
                NameColumn
            );

            if (students.Count == 0)
            {
                await ShowMessageAsync("警告", "没有找到有效的学生数据。");
                return;
            }

            // 自动保存到数据库
            var studentStore = new StudentStore(_baseDir);
            var existingStudents = studentStore.Load();
            var allStudents = new List<Student>(existingStudents);

            int addedCount = 0;
            int updatedCount = 0;

            foreach (var newStudent in students)
            {
                // 检查是否已存在（按学号或姓名）
                var existing = allStudents.FirstOrDefault(s => 
                    (!string.IsNullOrEmpty(s.Id) && s.Id == newStudent.Id) ||
                    (!string.IsNullOrEmpty(s.Name) && s.Name == newStudent.Name)
                );

                if (existing != null)
                {
                    // 更新现有学生
                    existing.Class = newStudent.Class;
                    existing.Id = newStudent.Id;
                    existing.Name = newStudent.Name;
                    existing.NamePinyin = newStudent.NamePinyin;
                    existing.NamePinyinInitials = newStudent.NamePinyinInitials;
                    updatedCount++;
                }
                else
                {
                    // 添加新学生
                    allStudents.Add(newStudent);
                    addedCount++;
                }
            }

            // 保存到数据库
            studentStore.Save(allStudents);

            // 显示成功消息
            await ShowMessageAsync("导入成功", 
                $"成功导入学生数据！\n\n新增：{addedCount} 人\n更新：{updatedCount} 人\n总计：{allStudents.Count} 人");

            this.Close(true);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("导入失败", $"导入过程中发生错误：\n{ex.Message}");
        }
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var msgBox = new Window
        {
            Title = title,
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 15,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new Button 
                    { 
                        Content = "确定", 
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        MinWidth = 80
                    }
                }
            }
        };

        if (msgBox.Content is StackPanel panel && panel.Children.LastOrDefault() is Button btn)
        {
            btn.Click += (_, __) => msgBox.Close();
        }

        await msgBox.ShowDialog(this);
    }

    private static string[][] ReadTabularInternal(string path)
    {
        try
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".xls" || ext == ".xlsx")
            {
                // 对于Excel文件，我们通过CsvImporter来读取
                // 这里使用一个临时的导入来获取数据结构
                var tempStudents = CsvImporter.ImportStudents(path, false, 0, 1, 2);
                // 但这种方法不够好，我们需要原始数据
                // 暂时回退到CSV读取方式，实际项目中应该扩展CsvImporter的API
            }
            
            // CSV文件读取
            var lines = File.ReadAllLines(path);
            return lines.Select(line => 
            {
                // 简单的CSV解析，不处理引号内的逗号
                return line.Split(',').Select(cell => cell.Trim()).ToArray();
            }).ToArray();
        }
        catch
        {
            return Array.Empty<string[]>();
        }
    }

    public (string? path, bool header, int classCol, int idCol, int nameCol, int firstDataRow) GetResult()
        => (SelectedPath, FirstRowIsHeader, ClassColumn, IdColumn, NameColumn, FirstDataRow);
}
