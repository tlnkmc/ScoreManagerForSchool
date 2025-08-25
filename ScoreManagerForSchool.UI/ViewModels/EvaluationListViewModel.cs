using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using ScoreManagerForSchool.Core.Storage;
using ScoreManagerForSchool.Core.Logging;
using ScoreManagerForSchool.UI.Services;

namespace ScoreManagerForSchool.UI.ViewModels
{
    public class EvaluationListViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly string _baseDir;
        private List<EvaluationEntry> _allEvaluations = new();

        public ObservableCollection<EvaluationEntry> Evaluations { get; } = new();
        public ObservableCollection<string> ClassOptions { get; } = new();

        private string? _query;
        public string? Query
        {
            get => _query;
            set { _query = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Query))); FilterEvaluations(); }
        }

        private string? _selectedClass;
        public string? SelectedClass
        {
            get => _selectedClass;
            set { _selectedClass = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedClass))); FilterEvaluations(); }
        }

        private DateTimeOffset? _startDate;
        public DateTimeOffset? StartDate
        {
            get => _startDate;
            set { _startDate = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartDate))); FilterEvaluations(); }
        }

        private DateTimeOffset? _endDate;
        public DateTimeOffset? EndDate
        {
            get => _endDate;
            set { _endDate = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EndDate))); FilterEvaluations(); }
        }

        private bool _showNegativeScoresOnly;
        public bool ShowNegativeScoresOnly
        {
            get => _showNegativeScoresOnly;
            set { _showNegativeScoresOnly = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowNegativeScoresOnly))); FilterEvaluations(); }
        }

        private EvaluationEntry? _selectedEvaluation;
        public EvaluationEntry? SelectedEvaluation
        {
            get => _selectedEvaluation;
            set { _selectedEvaluation = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedEvaluation))); }
        }

        private string? _editingId;
        public string? EditingId
        {
            get => _editingId;
            set { _editingId = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EditingId))); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand AddCommand { get; }

        public EvaluationListViewModel(string? baseDir = null)
        {
            try
            {
                Logger.LogInfo("EvaluationListViewModel 初始化开始", "EvaluationListViewModel");
                
                _baseDir = string.IsNullOrEmpty(baseDir) ? Path.Combine(Directory.GetCurrentDirectory(), "base") : baseDir;
                
                RefreshCommand = new RelayCommand(_ => LoadEvaluations());
                EditCommand = new RelayCommand(p => EditEvaluation(p as EvaluationEntry));
                SaveCommand = new RelayCommand(p => SaveEvaluation(p as EvaluationEntry));
                CancelCommand = new RelayCommand(_ => CancelEdit());
                DeleteCommand = new RelayCommand(p => DeleteEvaluation(p as EvaluationEntry));
                AddCommand = new RelayCommand(_ => AddNewEvaluation());

                // 设置默认选择
                SelectedClass = "全部班级";

                LoadEvaluations();
                
                Logger.LogInfo("EvaluationListViewModel 初始化完成", "EvaluationListViewModel");
            }
            catch (Exception ex)
            {
                Logger.LogError("EvaluationListViewModel 初始化失败", "EvaluationListViewModel", ex);
                ErrorHandler.HandleError(ex, "积分记录管理模块初始化失败", "EvaluationListViewModel.Constructor");
                throw;
            }
        }

        private void LoadEvaluations()
        {
            try
            {
                Logger.LogInfo("加载积分记录开始", "EvaluationListViewModel");
                
                var store = new EvaluationStore(_baseDir);
                _allEvaluations = store.Load();
                
                // 更新班级选项
                ClassOptions.Clear();
                ClassOptions.Add("全部班级"); // 添加"全部"选项
                var classes = _allEvaluations
                    .Where(e => !string.IsNullOrWhiteSpace(e.Class))
                    .Select(e => e.Class!)
                    .Distinct()
                    .OrderBy(c => c);
                foreach (var cls in classes)
                {
                    ClassOptions.Add(cls);
                }
                
                FilterEvaluations();
                
                Logger.LogInfo($"加载积分记录完成，共 {_allEvaluations.Count} 条记录", "EvaluationListViewModel");
            }
            catch (Exception ex)
            {
                Logger.LogError("加载积分记录失败", "EvaluationListViewModel", ex);
                ErrorHandler.HandleError(ex, "加载积分记录时发生错误", "EvaluationListViewModel.LoadEvaluations");
            }
        }

        private void FilterEvaluations()
        {
            try
            {
                Logger.LogDebug("开始筛选积分记录", "EvaluationListViewModel");
                
                Evaluations.Clear();
                
                var query = Query?.Trim() ?? string.Empty;
                var filtered = _allEvaluations.AsEnumerable();

                // 文本搜索筛选
                if (!string.IsNullOrWhiteSpace(query))
                {
                    filtered = filtered.Where(e =>
                        (e.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
                        (e.Class?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
                        (e.StudentId?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
                        (e.Remark?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
                        (e.TeacherName?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
                    );
                }

                // 班级筛选
                if (!string.IsNullOrWhiteSpace(SelectedClass) && SelectedClass != "全部班级")
                {
                    filtered = filtered.Where(e => string.Equals(e.Class, SelectedClass, StringComparison.OrdinalIgnoreCase));
                }

                // 时间范围筛选
                if (StartDate.HasValue)
                {
                    filtered = filtered.Where(e => e.Date >= StartDate.Value.DateTime.Date);
                }
                if (EndDate.HasValue)
                {
                    filtered = filtered.Where(e => e.Date <= EndDate.Value.DateTime.Date.AddDays(1).AddTicks(-1));
                }

                // 负分筛选
                if (ShowNegativeScoresOnly)
                {
                    filtered = filtered.Where(e => e.Score < 0);
                }

                // 按日期倒序排列，最新的在前
                var ordered = filtered.OrderByDescending(e => e.Date);
                
                foreach (var evaluation in ordered)
                {
                    // 确保每个评价条目都有唯一ID用于编辑
                    if (string.IsNullOrEmpty(evaluation.Id))
                    {
                        evaluation.Id = Guid.NewGuid().ToString();
                    }
                    Evaluations.Add(evaluation);
                }
                
                Logger.LogDebug($"筛选完成，显示 {Evaluations.Count} 条记录", "EvaluationListViewModel");
            }
            catch (Exception ex)
            {
                Logger.LogError("筛选积分记录失败", "EvaluationListViewModel", ex);
                ErrorHandler.HandleError(ex, "筛选积分记录时发生错误", "EvaluationListViewModel.FilterEvaluations");
            }
        }

        private void EditEvaluation(EvaluationEntry? evaluation)
        {
            if (evaluation == null) return;
            EditingId = evaluation.Id;
        }

        private void SaveEvaluation(EvaluationEntry? evaluation)
        {
            try
            {
                if (evaluation == null) 
                {
                    Logger.LogWarning("SaveEvaluation: 评价条目为空", "EvaluationListViewModel");
                    return;
                }
                
                if (!ScoreManagerForSchool.UI.Security.AuthManager.Ensure(_baseDir)) 
                {
                    Logger.LogWarning("SaveEvaluation: 身份验证失败", "EvaluationListViewModel");
                    return;
                }

                Logger.LogInfo($"保存评价条目: {evaluation.Name}", "EvaluationListViewModel");
                
                var store = new EvaluationStore(_baseDir);
                store.Save(_allEvaluations);
                
                EditingId = null;
                LoadEvaluations();
                
                Logger.LogInfo("评价条目保存成功", "EvaluationListViewModel");
            }
            catch (Exception ex)
            {
                Logger.LogError("保存评价条目失败", "EvaluationListViewModel", ex);
                ErrorHandler.HandleError(ex, "保存积分记录时发生错误", "EvaluationListViewModel.SaveEvaluation");
            }
        }

        private void CancelEdit()
        {
            EditingId = null;
            LoadEvaluations(); // 重新加载以撤销未保存的更改
        }

        private void DeleteEvaluation(EvaluationEntry? evaluation)
        {
            if (evaluation == null) return;
            
            if (!ScoreManagerForSchool.UI.Security.AuthManager.Ensure(_baseDir)) return;

            _allEvaluations.Remove(evaluation);
            var store = new EvaluationStore(_baseDir);
            store.Save(_allEvaluations);
            
            LoadEvaluations();
        }

        private void AddNewEvaluation()
        {
            if (!ScoreManagerForSchool.UI.Security.AuthManager.Ensure(_baseDir)) return;

            var newEvaluation = new EvaluationEntry
            {
                Id = Guid.NewGuid().ToString(),
                Date = DateTime.Now,
                Class = "",
                Name = "",
                Remark = "",
                Score = 0
            };

            _allEvaluations.Add(newEvaluation);
            var store = new EvaluationStore(_baseDir);
            store.Save(_allEvaluations);

            LoadEvaluations();
            EditingId = newEvaluation.Id;
        }
    }
}
