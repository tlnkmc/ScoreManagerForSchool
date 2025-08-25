using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ScoreManagerForSchool.Core.Storage;
using Avalonia.Platform.Storage;

namespace ScoreManagerForSchool.UI.ViewModels
{
    public class TeacherManagementViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly string _baseDir;
        private readonly TeacherStore _teacherStore;
        private readonly SubjectGroupStore _subjectGroupStore;
        private readonly EnhancedEvaluationService _evaluationService;

        public ObservableCollection<Teacher> Teachers { get; } = new();
        public ObservableCollection<SubjectGroup> SubjectGroups { get; } = new();
    public ObservableCollection<string> SubjectGroupNames { get; } = new();

        public ICommand ImportTeachersCommand { get; }
        public ICommand ExportTemplateCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand AddTeacherCommand { get; }
        public ICommand DeleteTeacherCommand { get; }

        // 文件操作回调
        public Func<Task<string?>>? SelectImportFileCallback { get; set; }
        public Func<string, Task<string?>>? SelectExportFileCallback { get; set; }

        public TeacherManagementViewModel(string? baseDir = null)
        {
            _baseDir = string.IsNullOrEmpty(baseDir) ? Path.Combine(Directory.GetCurrentDirectory(), "base") : baseDir;
            _teacherStore = new TeacherStore(_baseDir);
            _subjectGroupStore = new SubjectGroupStore(_baseDir);
            _evaluationService = new EnhancedEvaluationService(_baseDir);

            ImportTeachersCommand = new RelayCommand(async _ => await ImportTeachersAsync());
            ExportTemplateCommand = new RelayCommand(async _ => await ExportTemplateAsync());
            RefreshCommand = new RelayCommand(_ => LoadData());
            AddTeacherCommand = new RelayCommand(_ => AddNewTeacher());
            DeleteTeacherCommand = new RelayCommand(param => DeleteTeacher(param as Teacher));

            LoadData();
        }

        private void LoadData()
        {
            Teachers.Clear();
            foreach (var teacher in _teacherStore.Load())
            {
                Teachers.Add(teacher);
            }

            ReloadSubjectGroups();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Teachers)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SubjectGroups)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SubjectGroupNames)));
        }

        public void ReloadSubjectGroups()
        {
            SubjectGroups.Clear();
            foreach (var group in _subjectGroupStore.Load())
            {
                SubjectGroups.Add(group);
            }
            SubjectGroupNames.Clear();
            foreach (var name in SubjectGroups.Select(g => g.Name).Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                SubjectGroupNames.Add(name!);
            }
        }

        private async Task ImportTeachersAsync()
        {
            try
            {
                if (SelectImportFileCallback == null)
                {
                    StatusMessage = "文件选择功能未初始化";
                    return;
                }

                var filePath = await SelectImportFileCallback();
                if (string.IsNullOrEmpty(filePath))
                {
                    return; // 用户取消了选择
                }

                List<Teacher> importedTeachers;

                if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    importedTeachers = CsvImporter.ImportTeachers(filePath, true);
                }
                else
                {
                    // 对于Excel文件，暂时不支持
                    StatusMessage = "暂不支持Excel格式，请使用CSV文件";
                    return;
                }

                _evaluationService.ImportTeachers(importedTeachers);
                LoadData();

                StatusMessage = $"成功导入 {importedTeachers.Count} 位教师";
            }
            catch (Exception ex)
            {
                StatusMessage = $"导入失败：{ex.Message}";
            }
        }

        private async Task ExportTemplateAsync()
        {
            try
            {
                if (SelectExportFileCallback == null)
                {
                    StatusMessage = "文件选择功能未初始化";
                    return;
                }

                var filePath = await SelectExportFileCallback("teachers_template.csv");
                if (string.IsNullOrEmpty(filePath))
                {
                    return; // 用户取消了选择
                }

                var template = "姓名,科目,科目组,班级列表\n";
                template += "张老师,数学,数学,一班;二班\n";
                template += "李老师,语文,语文,一班\n";
                template += "王老师,英语,英语,二班;三班\n";

                await File.WriteAllTextAsync(filePath, template, System.Text.Encoding.UTF8);
                StatusMessage = "模板文件导出成功";
            }
            catch (Exception ex)
            {
                StatusMessage = $"导出失败：{ex.Message}";
            }
        }

        private void AddNewTeacher()
        {
            var newTeacher = new Teacher
            {
                Name = "新教师",
                Subject = "",
                SubjectGroup = "",
                Classes = new List<string>()
            };

            if (!string.IsNullOrEmpty(newTeacher.Name))
            {
                var (pinyin, initials) = PinyinUtil.MakeKeys(newTeacher.Name);
                newTeacher.NamePinyin = pinyin;
                newTeacher.NamePinyinInitials = initials;
            }

            Teachers.Add(newTeacher);
            SelectedTeacher = newTeacher;
        }

        public void AddTeacher(Teacher teacher)
        {
            if (teacher != null)
            {
                Teachers.Add(teacher);
                SelectedTeacher = teacher;
                StatusMessage = "教师添加成功";
            }
        }

        public void DeleteTeacher(Teacher? teacher)
        {
            if (teacher != null && Teachers.Contains(teacher))
            {
                Teachers.Remove(teacher);
                SaveTeachers();
                StatusMessage = $"已删除教师：{teacher.Name}";
            }
        }

        private void SaveTeachers()
        {
            _teacherStore.Save(Teachers);
        }

        public void SaveCurrentTeacher()
        {
            if (SelectedTeacher != null)
            {
                // 更新拼音字段
                if (!string.IsNullOrEmpty(SelectedTeacher.Name))
                {
                    var (pinyin, initials) = PinyinUtil.MakeKeys(SelectedTeacher.Name);
                    SelectedTeacher.NamePinyin = pinyin;
                    SelectedTeacher.NamePinyinInitials = initials;
                }

                SaveTeachers();
                StatusMessage = $"已保存教师：{SelectedTeacher.Name}";
            }
        }

        // 绑定属性
        private Teacher? _selectedTeacher;
        public Teacher? SelectedTeacher
        {
            get => _selectedTeacher;
            set
            {
                _selectedTeacher = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTeacher)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTeacherSelected)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ClassesText)));
            }
        }

        public bool IsTeacherSelected => SelectedTeacher != null;

        // 班级列表的文本表示
        public string ClassesText
        {
            get => SelectedTeacher?.Classes != null ? string.Join(";", SelectedTeacher.Classes) : string.Empty;
            set
            {
                if (SelectedTeacher != null)
                {
                    SelectedTeacher.Classes = value?.Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .Select(c => c.Trim())
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .ToList() ?? new List<string>();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ClassesText)));
                }
            }
        }

        private string? _statusMessage;
        public string? StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusMessage)));
            }
        }

        // 搜索功能
        private string? _searchQuery;
        public string? SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchQuery)));
                FilterTeachers();
            }
        }

        private void FilterTeachers()
        {
            Teachers.Clear();
            var allTeachers = _teacherStore.Load();
            
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                foreach (var teacher in allTeachers)
                {
                    Teachers.Add(teacher);
                }
            }
            else
            {
                var filtered = _teacherStore.SearchByName(SearchQuery);
                foreach (var teacher in filtered)
                {
                    Teachers.Add(teacher);
                }
            }
        }
    }
}
