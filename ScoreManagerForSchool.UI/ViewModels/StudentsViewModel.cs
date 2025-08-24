using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using ScoreManagerForSchool.Core.Storage;
using ScoreManagerForSchool.UI.Security;

namespace ScoreManagerForSchool.UI.ViewModels;

public class StudentsViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private List<Student> _allStudents = new List<Student>();
    private List<Student> _students = new List<Student>();
    public List<Student> Students { get => _students; private set { _students = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Students))); } }

    // inline edit state
    public string? EditingId { get; private set; }

    // paging
    public int PageSize { get; set; } = 25;
    private int _page = 1;
    public int Page
    {
        get => _page;
        set { _page = Math.Max(1, value); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Page))); RefreshView(); }
    }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((_allStudents?.Count ?? 0) / (double)PageSize));

    // search/filter
    private string? _query;
    public string? Query { get => _query; set { _query = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Query))); ApplyFilter(); } }

    // selection/edit
    private Student? _selectedStudent;
    public Student? SelectedStudent { get => _selectedStudent; set { _selectedStudent = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedStudent))); } }

    public ICommand LoadCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PrevPageCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SaveEditCommand { get; }

    // row actions
    public ICommand BeginEditCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand SaveRowCommand { get; }
    public ICommand DeleteRowCommand { get; }

    private readonly string _baseDir;

    public StudentsViewModel() : this(null) { }

    // allow injection of base directory for testing or custom storage locations
    public StudentsViewModel(string? baseDir)
    {
        _baseDir = string.IsNullOrEmpty(baseDir) ? Path.Combine(Directory.GetCurrentDirectory(), "base") : baseDir;

        LoadCommand = new RelayCommand(_ => Load());
        ImportCommand = new RelayCommand(param => ImportCsv(param as string));
        NextPageCommand = new RelayCommand(_ => { if (Page < TotalPages) Page++; });
        PrevPageCommand = new RelayCommand(_ => { if (Page > 1) Page--; });
    DeleteCommand = new RelayCommand(_ => { if (!ScoreManagerForSchool.UI.Security.AuthManager.Ensure(_baseDir)) return; DeleteSelected(); });
    SaveEditCommand = new RelayCommand(_ => { if (!ScoreManagerForSchool.UI.Security.AuthManager.Ensure(_baseDir)) return; SaveSelected(); });

        BeginEditCommand = new RelayCommand(p => BeginEdit(p as Student));
        CancelEditCommand = new RelayCommand(p => CancelEdit(p as Student));
    SaveRowCommand = new RelayCommand(p => { if (!ScoreManagerForSchool.UI.Security.AuthManager.Ensure(_baseDir)) return; SaveRow(p as Student); });
    DeleteRowCommand = new RelayCommand(p => { if (!ScoreManagerForSchool.UI.Security.AuthManager.Ensure(_baseDir)) return; DeleteRow(p as Student); });

        Load();
    }

    private void BeginEdit(Student? row)
    {
        if (row == null) return;
        EditingId = row.Id;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EditingId)));
    }

    private void CancelEdit(Student? row)
    {
        EditingId = null;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EditingId)));
        Load();
    }

    private void SaveRow(Student? row)
    {
        if (row == null) return;
        var store = new StudentStore(_baseDir);
        var list = store.Load();
        var idx = list.FindIndex(s => s.Id == row.Id);
        var full = ScoreManagerForSchool.Core.Storage.PinyinUtil.Full(row.Name);
        var init = ScoreManagerForSchool.Core.Storage.PinyinUtil.Initials(row.Name);
        var newItem = new Student { Class = row.Class, Id = row.Id, Name = row.Name, NamePinyin = full, NamePinyinInitials = init };
        if (idx >= 0) list[idx] = newItem;
        else list.Add(newItem);
        store.Save(list);
        EditingId = null;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EditingId)));
        Load();
    }

    private void DeleteRow(Student? row)
    {
        if (row == null) return;
        var store = new StudentStore(_baseDir);
        var list = store.Load();
        list.RemoveAll(s => s.Id == row.Id);
        store.Save(list);
        Load();
    }

    private void ApplyFilter()
    {
        IEnumerable<Student> q = _allStudents;
        if (!string.IsNullOrWhiteSpace(Query))
        {
            var needle = Query.Trim();
            // 检查输入是否为纯ASCII字母（可能是拼音）
            var isAsciiLetters = needle.All(c => char.IsLetter(c) && c <= 127);
            var needleLower = needle.ToLowerInvariant();
            
            q = q.Where(s => (s.Name ?? "").Contains(needle, StringComparison.OrdinalIgnoreCase)
                         || (s.Id ?? "").Contains(needle, StringComparison.OrdinalIgnoreCase)
                         || (s.Class ?? "").Contains(needle, StringComparison.OrdinalIgnoreCase)
                         // 拼音搜索：支持全拼和首字母匹配
                         || (isAsciiLetters && !string.IsNullOrEmpty(s.NamePinyin) && s.NamePinyin.Contains(needleLower, StringComparison.Ordinal))
                         || (isAsciiLetters && !string.IsNullOrEmpty(s.NamePinyinInitials) && s.NamePinyinInitials.Contains(needleLower, StringComparison.Ordinal)));
        }
        _filtered = q.ToList();
        Page = 1;
        RefreshView();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalPages)));
    }

    private List<Student> _filtered = new List<Student>();

    private void RefreshView()
    {
        var skip = (Page - 1) * PageSize;
        Students = _filtered.Skip(skip).Take(PageSize).ToList();
    }

    private Student CloneStudent(Student s) => new Student { Class = s.Class, Id = s.Id, Name = s.Name, NamePinyin = ScoreManagerForSchool.Core.Storage.PinyinUtil.Full(s.Name), NamePinyinInitials = ScoreManagerForSchool.Core.Storage.PinyinUtil.Initials(s.Name) };

    public void Load()
    {
        var store = new StudentStore(_baseDir);
        _allStudents = store.Load() ?? new List<Student>();
        _filtered = new List<Student>(_allStudents);
        Page = 1;
        RefreshView();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalPages)));
    }

    public void ImportCsv(string? path)
    {
        ImportCsv(path, true);
    }

    public void ImportCsv(string? path, bool firstRowIsHeader)
    {
        try
        {
            if (string.IsNullOrEmpty(path)) return;
            path = path.Trim();
            if ((path.StartsWith('"') && path.EndsWith('"')) || (path.StartsWith('\'') && path.EndsWith('\'')))
            {
                path = path.Substring(1, path.Length - 2);
            }
            if (!File.Exists(path)) return;
            var imported = ScoreManagerForSchool.Core.Storage.CsvImporter.ImportStudents(path, firstRowIsHeader);
            _allStudents = new List<Student>(imported);
            _filtered = new List<Student>(_allStudents);
            Page = 1;
            RefreshView();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalPages)));
        }
        catch (Exception ex)
        {
            try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "smfs_preview_error.log"), DateTime.UtcNow.ToString("o") + " " + ex + "\n"); } catch { }
        }
    }

    public void ApplyImported(List<Student> imported)
    {
        var store = new StudentStore(_baseDir);
        store.Save(imported);
        Load();
    }

    private void DeleteSelected()
    {
        if (SelectedStudent == null) return;
        var store = new StudentStore(_baseDir);
        var list = store.Load();
        var toRemove = list.FirstOrDefault(s => s.Id == SelectedStudent.Id && s.Class == SelectedStudent.Class && s.Name == SelectedStudent.Name);
        if (toRemove != null)
        {
            list.Remove(toRemove);
            store.Save(list);

            _allStudents = list;
            _filtered = new List<Student>(_allStudents);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalPages)));
            RefreshView();
        }
    }

    private void SaveSelected()
    {
        if (SelectedStudent == null) return;
        var store = new StudentStore(_baseDir);
        var list = store.Load();
        var idx = list.FindIndex(s => s.Id == SelectedStudent.Id);
        var full = ScoreManagerForSchool.Core.Storage.PinyinUtil.Full(SelectedStudent.Name);
        var init = ScoreManagerForSchool.Core.Storage.PinyinUtil.Initials(SelectedStudent.Name);
        if (idx >= 0)
        {
            list[idx].Class = SelectedStudent.Class;
            list[idx].Name = SelectedStudent.Name;
            list[idx].Id = SelectedStudent.Id;
            list[idx].NamePinyin = full;
            list[idx].NamePinyinInitials = init;
        }
        else
        {
            list.Add(new Student { Class = SelectedStudent.Class, Id = SelectedStudent.Id, Name = SelectedStudent.Name, NamePinyin = full, NamePinyinInitials = init });
        }
        store.Save(list);

        _allStudents = list;
        _filtered = new List<Student>(_allStudents);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalPages)));
        RefreshView();
    }
}

