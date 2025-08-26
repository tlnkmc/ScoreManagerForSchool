using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using ScoreManagerForSchool.Core.Storage;

namespace ScoreManagerForSchool.UI.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly string _baseDir;

        public int StudentCount { get; private set; }
        public int ClassCount { get; private set; }
        public int PendingCount { get; private set; }
        public int EvaluationCount { get; private set; }
        public int CriticalCount { get; private set; }

        public ObservableCollection<EvaluationEntry> Recent { get; } = new();
        public ObservableCollection<StatsSummaryItem> CriticalStudents { get; } = new();

    public ObservableCollection<string> RangeOptions { get; } = new(new[] { "全部", "7天", "30天", "90天" });
    private string _selectedRange = "全部";
    public string SelectedRange { get => _selectedRange; set { if (_selectedRange != value) { _selectedRange = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedRange))); ApplyRecent(); } } }

        public ICommand RefreshCommand { get; }

        public HomeViewModel(string? baseDir = null)
        {
            _baseDir = string.IsNullOrEmpty(baseDir) ? Path.Combine(Directory.GetCurrentDirectory(), "base") : baseDir;
            RefreshCommand = new RelayCommand(_ => Load());
            Load();
        }

        public void Load()
        {
            var sstore = new StudentStore(_baseDir);
            var cstore = new ClassStore(_baseDir);
            var schstore = new SchemeStore(_baseDir);
            var estore = new EvaluationStore(_baseDir);

            var students = sstore.Load();
            var classes = cstore.Load();
            var schemes = schstore.Load();
            var evals = estore.Load();

            StudentCount = students?.Count ?? 0;
            ClassCount = classes?.Count ?? 0;
            // 计算待处理项（没有学生姓名的评价记录）
            var pendingItems = evals?.Where(e => string.IsNullOrWhiteSpace(e.Name)).ToList() ?? [];
            PendingCount = pendingItems.Count;
            EvaluationCount = evals?.Count ?? 0;
            
            // 计算关键积分学生
            var criticalStudents = CalculateCriticalStudents(evals ?? [], students ?? []);
            CriticalCount = criticalStudents.Count;

            // recent by selected range
            _evalsCache = evals ?? [];
            ApplyRecent();

            // 更新关键积分学生列表（显示前5个）
            CriticalStudents.Clear();
            foreach (var critical in criticalStudents.Take(5))
            {
                CriticalStudents.Add(critical);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StudentCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ClassCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PendingCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EvaluationCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CriticalCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Recent)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CriticalStudents)));
        }

        private List<EvaluationEntry> _evalsCache = new();

        private void ApplyRecent()
        {
            if (_evalsCache == null) return;
            DateTime? cutoff = SelectedRange switch
            {
                "7天" => DateTime.Now.AddDays(-7),
                "30天" => DateTime.Now.AddDays(-30),
                "90天" => DateTime.Now.AddDays(-90),
                _ => null
            };
            var q = _evalsCache.AsEnumerable();
            if (cutoff != null) q = q.Where(e => e.Date >= cutoff.Value);
            // 过滤掉待处理项（没有学生姓名的记录）
            q = q.Where(e => !string.IsNullOrWhiteSpace(e.Name));
            q = q.OrderByDescending(e => e.Date).Take(8);
            Recent.Clear();
            foreach (var e in q) Recent.Add(e);
        }

        private List<StatsSummaryItem> CalculateCriticalStudents(List<EvaluationEntry> evaluations, List<Student> students)
        {
            // 按学生分组计算总积分
            var groups = evaluations
                .Where(e => !string.IsNullOrWhiteSpace(e.Name) || !string.IsNullOrWhiteSpace(e.StudentId))
                .GroupBy(e => new
                {
                    Id = !string.IsNullOrWhiteSpace(e.StudentId) ? e.StudentId : LookupId(e.Name, e.Class, students),
                    e.Name,
                    e.Class
                });

            var criticalStudents = groups
                .Select(g => new StatsSummaryItem
                {
                    Class = g.Key.Class,
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    TotalScore = g.Sum(x => x.Score),
                    Count = g.Count(),
                    IsCritical = CriticalScoreLevels.IsCritical(g.Sum(x => x.Score))
                })
                .Where(item => item.IsCritical)
                .OrderBy(item => item.TotalScore) // 按分数从低到高排序
                .ToList();

            return criticalStudents;
        }

        private string? LookupId(string? name, string? klass, List<Student> students)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var st = students.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)
                                                && (string.IsNullOrWhiteSpace(klass) || string.Equals(s.Class, klass, StringComparison.OrdinalIgnoreCase)));
            return st?.Id;
        }
    }
}
