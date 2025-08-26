using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ScoreManagerForSchool.Core.Storage;
using ScoreManagerForSchool.UI.Views;

namespace ScoreManagerForSchool.UI.ViewModels
{
    public class StatsSummaryItem
    {
        public string? Class { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public double TotalScore { get; set; }
        public int Count { get; set; }
        public bool IsCritical { get; set; }
        public CriticalScoreLevel? CriticalLevel { get; set; }
    }

    public enum SortKey { Score, Class, Id, Name }

    public class StatsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly string _baseDir;
        private List<EvaluationEntry> _allEvaluations = new();
        private List<Student> _allStudents = new();

        public ObservableCollection<EvaluationEntry> PendingTop { get; } = new();
        public ObservableCollection<StatsSummaryItem> Summary { get; } = new();
        public ObservableCollection<StatsSummaryItem> CriticalStudents { get; } = new();

        public SortKey SortBy { get; private set; } = SortKey.Score;
        public bool SortDesc { get; private set; } = true;

        public ICommand RefreshCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ProcessPendingCommand { get; }
        public ICommand DeletePendingCommand { get; }
        public ICommand SortCommand { get; }
        public ICommand EditEvaluationCommand { get; }
        public ICommand DeleteEvaluationCommand { get; }
        public ICommand OpenCriticalLevelsCommand { get; }

        public StatsViewModel(string? baseDir = null)
        {
            _baseDir = string.IsNullOrEmpty(baseDir) ? Path.Combine(Directory.GetCurrentDirectory(), "base") : baseDir;
            RefreshCommand = new RelayCommand(_ => Load());
            ExportCommand = new RelayCommand(async _ => await ExportWithDialog());
            ProcessPendingCommand = new RelayCommand(p => ProcessPending(p as EvaluationEntry));
            DeletePendingCommand = new RelayCommand(p => DeletePending(p as EvaluationEntry));
            SortCommand = new RelayCommand(p => ToggleSort(p as string));
            EditEvaluationCommand = new RelayCommand(p => EditEvaluation(p as EvaluationEntry));
            DeleteEvaluationCommand = new RelayCommand(p => DeleteEvaluation(p as EvaluationEntry));
            OpenCriticalLevelsCommand = new RelayCommand(_ => OpenCriticalLevelsWindow());
            Load();
        }

        private void ToggleSort(string? key)
        {
            if (string.IsNullOrEmpty(key)) return;
            var map = new Dictionary<string, SortKey>(StringComparer.OrdinalIgnoreCase)
            {
                ["Score"] = SortKey.Score,
                ["Class"] = SortKey.Class,
                ["Id"] = SortKey.Id,
                ["Name"] = SortKey.Name,
            };
            if (!map.TryGetValue(key, out var k)) return;
            if (SortBy == k)
                SortDesc = !SortDesc;
            else
            {
                SortBy = k;
                SortDesc = k == SortKey.Score; // 默认分数降序，其他升序
            }
            ApplySummary();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SortBy)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SortDesc)));
        }

        public void Load()
        {
            var estore = new EvaluationStore(_baseDir);
            _allEvaluations = estore.Load();
            _allStudents = new StudentStore(_baseDir).Load();

            PendingTop.Clear();
            foreach (var p in _allEvaluations.Where(e => string.IsNullOrWhiteSpace(e.Name)).Take(3))
                PendingTop.Add(p);

            ApplySummary();
        }

        private void ApplySummary()
        {
            Summary.Clear();
            // group by identity: prefer Id if any, otherwise Name+Class
            var groups = _allEvaluations
                .Where(e => !string.IsNullOrWhiteSpace(e.Name) || !string.IsNullOrWhiteSpace(e.StudentId))
                .GroupBy(e => new
                {
                    Id = !string.IsNullOrWhiteSpace(e.StudentId) ? e.StudentId : LookupId(e.Name, e.Class),
                    e.Name,
                    e.Class
                });

            var items = groups.Select(g => 
            {
                var totalScore = g.Sum(x => x.Score);
                var criticalLevel = CriticalScoreLevels.GetCriticalLevel(totalScore);
                return new StatsSummaryItem
                {
                    Class = g.Key.Class,
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    TotalScore = totalScore,
                    Count = g.Count(),
                    IsCritical = criticalLevel != null,
                    CriticalLevel = criticalLevel
                };
            });

            // Apply selected sort with stable tie-breakers
            IEnumerable<StatsSummaryItem> ordered;
            switch (SortBy)
            {
                case SortKey.Class:
                    ordered = SortDesc ? items.OrderByDescending(i => i.Class) : items.OrderBy(i => i.Class);
                    break;
                case SortKey.Id:
                    ordered = SortDesc ? items.OrderByDescending(i => i.Id) : items.OrderBy(i => i.Id);
                    break;
                case SortKey.Name:
                    ordered = SortDesc ? items.OrderByDescending(i => i.Name) : items.OrderBy(i => i.Name);
                    break;
                default:
                    ordered = SortDesc ? items.OrderByDescending(i => i.TotalScore) : items.OrderBy(i => i.TotalScore);
                    break;
            }
            // tie-breakers to keep deterministic
            ordered = ((IOrderedEnumerable<StatsSummaryItem>)ordered)
                .ThenBy(i => i.Class)
                .ThenBy(i => i.Id)
                .ThenBy(i => i.Name);

            foreach (var it in ordered)
                Summary.Add(it);

            // 更新关键积分学生列表
            CriticalStudents.Clear();
            var criticalStudents = ordered.Where(item => item.IsCritical).Take(10); // 最多显示10个关键学生
            foreach (var critical in criticalStudents)
                CriticalStudents.Add(critical);
        }

        private string? LookupId(string? name, string? klass)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var st = _allStudents.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)
                                                   && (string.IsNullOrWhiteSpace(klass) || string.Equals(s.Class, klass, StringComparison.OrdinalIgnoreCase)));
            return st?.Id;
        }

        private void ProcessPending(EvaluationEntry? e)
        {
            if (!ScoreManagerForSchool.UI.Security.AuthManager.Ensure(_baseDir)) return;
            if (e == null) return;
            // 简化：若无姓名，尝试从学生表按原因中的姓名提取第一个中文/英文名字（可后续加强为 NLP）
            if (string.IsNullOrWhiteSpace(e.Name))
            {
                var m = System.Text.RegularExpressions.Regex.Match(e.Remark ?? string.Empty, "[\u4e00-\u9fa5A-Za-z]{2,}");
                if (m.Success) e.Name = m.Value;
            }
            if (string.IsNullOrWhiteSpace(e.Class) && !string.IsNullOrWhiteSpace(e.Name))
            {
                var candidate = _allStudents.FirstOrDefault(s => string.Equals(s.Name, e.Name, StringComparison.OrdinalIgnoreCase));
                if (candidate != null) e.Class = candidate.Class;
            }

            // 标记为已处理（这里简单按推断填充；复杂流程可在后续引入交互窗口）
            SaveEvaluations();
            Load();
        }

        private void DeletePending(EvaluationEntry? e)
        {
            if (!ScoreManagerForSchool.UI.Security.AuthManager.Ensure(_baseDir)) return;
            if (e == null) return;
            _allEvaluations.Remove(e);
            SaveEvaluations();
            Load();
        }

        private void SaveEvaluations()
        {
            new EvaluationStore(_baseDir).Save(_allEvaluations);
        }

        private void EditEvaluation(EvaluationEntry? e)
        {
            if (!ScoreManagerForSchool.UI.Security.AuthManager.Ensure(_baseDir)) return;
            if (e == null) return;
            
            // 打开编辑对话框 - 这里暂时使用简单的编辑逻辑
            // 在实际实现中，需要在UI层显示编辑对话框
            EditEvaluationDialog?.Invoke(e);
        }

        private void DeleteEvaluation(EvaluationEntry? e)
        {
            if (!ScoreManagerForSchool.UI.Security.AuthManager.Ensure(_baseDir)) return;
            if (e == null) return;
            
            _allEvaluations.Remove(e);
            SaveEvaluations();
            Load();
        }

        // 编辑对话框处理函数，需要在View中设置
        public Action<EvaluationEntry>? EditEvaluationDialog { get; set; }

        // 导出对话框处理函数，需要在View中调用
        public Func<Task<(bool confirmed, DateTime startDate, DateTime endDate)>>? ShowExportDialog { get; set; }

        private async Task ExportWithDialog()
        {
            if (ShowExportDialog == null)
            {
                // 如果没有设置对话框回调，使用默认导出（全部数据）
                ExportCsv();
                return;
            }

            var (confirmed, startDate, endDate) = await ShowExportDialog();
            if (confirmed)
            {
                ExportCsv(startDate, endDate);
            }
        }

        private void ExportCsv()
        {
            ExportCsv(DateTime.MinValue, DateTime.MaxValue);
        }

        private void ExportCsv(DateTime startDate, DateTime endDate)
        {
            var dir = Path.Combine(_baseDir, "export");
            Directory.CreateDirectory(dir);
            
            // 过滤指定时间范围内的评价记录
            var filteredEvaluations = _allEvaluations
                .Where(e => e.Date >= startDate && e.Date <= endDate)
                .ToList();

            var rangeText = startDate == DateTime.MinValue ? "all" : $"{startDate:yyyyMMdd}-{endDate:yyyyMMdd}";
            var file = Path.Combine(dir, $"scores-{rangeText}-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
            
            var sb = new StringBuilder();
            sb.AppendLine("导出时间,班级,唯一号,姓名,记录时间,原因,分数");
            
            foreach (var eval in filteredEvaluations.OrderBy(e => e.Date))
            {
                // 尝试从学生表获取完整信息
                var student = _allStudents.FirstOrDefault(s => 
                    (!string.IsNullOrEmpty(eval.StudentId) && s.Id == eval.StudentId) ||
                    (string.IsNullOrEmpty(eval.StudentId) && s.Name == eval.Name && s.Class == eval.Class));
                
                sb.AppendLine(string.Join(',',
                    Escape(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                    Escape(student?.Class ?? eval.Class),
                    Escape(student?.Id ?? eval.StudentId),
                    Escape(student?.Name ?? eval.Name),
                    Escape(eval.Date.ToString("yyyy-MM-dd HH:mm:ss")),
                    Escape(eval.Remark),
                    eval.Score.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }
            
            // 添加统计汇总
            sb.AppendLine();
            sb.AppendLine("=== 统计汇总 ===");
            sb.AppendLine("班级,唯一号,姓名,累计积分,记录数");
            
            // 重新计算过滤时间范围内的统计
            var summaryGroups = filteredEvaluations
                .Where(e => !string.IsNullOrWhiteSpace(e.Name) || !string.IsNullOrWhiteSpace(e.StudentId))
                .GroupBy(e => new
                {
                    Id = !string.IsNullOrWhiteSpace(e.StudentId) ? e.StudentId : LookupId(e.Name, e.Class),
                    e.Name,
                    e.Class
                });

            var summaryItems = summaryGroups.Select(g => new
            {
                Class = g.Key.Class,
                Id = g.Key.Id,
                Name = g.Key.Name,
                TotalScore = g.Sum(x => x.Score),
                Count = g.Count(),
            }).OrderByDescending(x => x.TotalScore);

            foreach (var item in summaryItems)
            {
                sb.AppendLine(string.Join(',',
                    Escape(item.Class),
                    Escape(item.Id),
                    Escape(item.Name),
                    item.TotalScore.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    item.Count.ToString()));
            }

            File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
        }

        private static string Escape(string? s)
        {
            s ??= string.Empty;
            if (s.Contains(',') || s.Contains('"'))
            {
                s = '"' + s.Replace("\"", "\"\"") + '"';
            }
            return s;
        }

        private async void OpenCriticalLevelsWindow()
        {
            var window = new CriticalScoreLevelsWindow();
            var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
            if (mainWindow != null)
            {
                await window.ShowDialog(mainWindow);
            }
        }
    }
}
