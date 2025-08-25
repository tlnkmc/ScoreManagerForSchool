using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ScoreManagerForSchool.Core.Storage;
using ScoreManagerForSchool.UI.Security;

namespace ScoreManagerForSchool.UI.Views.Dialogs;

public partial class SubjectGroupManager : Window
{
    private readonly SubjectGroupStore _store;
    private readonly ObservableCollection<SubjectGroup> _items = new();
    private ListBox? _listBox;
    private readonly string _baseDir;

    public SubjectGroupManager() : this(Path.Combine(Directory.GetCurrentDirectory(), "base")) { }

    public SubjectGroupManager(string baseDir)
    {
        InitializeComponent();
        _baseDir = baseDir;
        _store = new SubjectGroupStore(baseDir);
        
        // 查找控件
        _listBox = this.FindControl<ListBox>("ItemsList");
        var addBtn = this.FindControl<Button>("AddBtn");
        var deleteBtn = this.FindControl<Button>("DeleteBtn");
        var resetBtn = this.FindControl<Button>("ResetBtn");
        var saveBtn = this.FindControl<Button>("SaveBtn");
        var closeBtn = this.FindControl<Button>("CloseBtn");
        
        // 设置数据源
        if (_listBox != null)
        {
            _listBox.ItemsSource = _items;
        }
        
        // 设置事件处理
        if (addBtn != null) addBtn.Click += OnAdd;
        if (deleteBtn != null) deleteBtn.Click += OnDelete;
        if (resetBtn != null) resetBtn.Click += OnReset;
        if (saveBtn != null) saveBtn.Click += OnSave;
        if (closeBtn != null) closeBtn.Click += (_, __) => this.Close(true);
        
        // 加载数据
        LoadData();
    }

    private void LoadData()
    {
        try
        {
            if (!AuthManager.Ensure(_baseDir)) return;
            
            _items.Clear();
            var groups = _store.Load();
            
            System.Console.WriteLine($"[DEBUG] 加载了 {groups.Count} 个科目组");
            
            foreach (var group in groups)
            {
                _items.Add(new SubjectGroup 
                { 
                    Name = group.Name ?? "未命名", 
                    Description = group.Description ?? "" 
                });
                System.Console.WriteLine($"[DEBUG] 科目组: {group.Name}");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[ERROR] 加载科目组数据时出错: {ex.Message}");
        }
    }

    private void OnAdd(object? sender, RoutedEventArgs e)
    {
        var newGroup = new SubjectGroup 
        { 
            Name = "新科目组", 
            Description = "" 
        };
        _items.Add(newGroup);
        
        // 选中新添加的项
        if (_listBox != null)
        {
            _listBox.SelectedItem = newGroup;
        }
        
        System.Console.WriteLine("[DEBUG] 添加了新科目组");
    }

    private void OnDelete(object? sender, RoutedEventArgs e)
    {
        if (_listBox?.SelectedItem is SubjectGroup selectedGroup)
        {
            _items.Remove(selectedGroup);
            System.Console.WriteLine($"[DEBUG] 删除科目组: {selectedGroup.Name}");
        }
    }

    private void OnDeleteItem(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is SubjectGroup group)
        {
            _items.Remove(group);
            System.Console.WriteLine($"[DEBUG] 删除科目组: {group.Name}");
        }
    }

    private void OnReset(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!AuthManager.Ensure(_baseDir)) return;
            _store.ResetToDefault();
            LoadData();
            System.Console.WriteLine("[DEBUG] 重置为默认科目组");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[ERROR] 重置科目组时出错: {ex.Message}");
        }
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!AuthManager.Ensure(_baseDir)) return;
            
            // 过滤掉空名称的项
            var validGroups = _items.Where(x => !string.IsNullOrWhiteSpace(x.Name));
            _store.Save(validGroups);
            
            System.Console.WriteLine($"[DEBUG] 已保存 {validGroups.Count()} 个科目组");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[ERROR] 保存科目组时出错: {ex.Message}");
        }
    }
}
