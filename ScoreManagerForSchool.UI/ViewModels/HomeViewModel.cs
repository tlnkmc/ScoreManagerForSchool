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
        public int SchemeCount { get; private set; }
        public int EvaluationCount { get; private set; }
    public int PendingCount { get; private set; }

    public ObservableCollection<EvaluationEntry> Recent { get; } = new();
    public ObservableCollection<EvaluationEntry> PendingTop { get; } = new();

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
            SchemeCount = schemes?.Count ?? 0;
            EvaluationCount = evals?.Count ?? 0;
            PendingCount = evals?.Count(e => string.IsNullOrWhiteSpace(e.Name)) ?? 0;

            // recent by selected range
            _evalsCache = evals ?? [];
            ApplyRecent();

            // pending top (most recent 5 without Name)
            PendingTop.Clear();
            foreach (var p in _evalsCache.Where(e => string.IsNullOrWhiteSpace(e.Name))
                                         .OrderByDescending(e => e.Date)
                                         .Take(5))
            {
                PendingTop.Add(p);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StudentCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ClassCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SchemeCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EvaluationCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PendingCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Recent)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PendingTop)));
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
            q = q.OrderByDescending(e => e.Date).Take(8);
            Recent.Clear();
            foreach (var e in q) Recent.Add(e);
        }
    }
}
