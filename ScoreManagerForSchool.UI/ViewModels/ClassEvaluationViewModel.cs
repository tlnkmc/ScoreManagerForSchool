using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using ScoreManagerForSchool.Core.Storage;

namespace ScoreManagerForSchool.UI.ViewModels
{
    public class ClassEvaluationRecord : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        
        private string? _class;
        public string? Class 
        { 
            get => _class; 
            set { _class = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Class))); } 
        }
        
        private string? _name;
        public string? Name 
        { 
            get => _name; 
            set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); } 
        }
        
        private string? _item;
        public string? Item 
        { 
            get => _item; 
            set { _item = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Item))); } 
        }
        
        private double _score;
        public double Score 
        { 
            get => _score; 
            set { _score = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Score))); } 
        }
        
        private string? _remark;
        public string? Remark 
        { 
            get => _remark; 
            set { _remark = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Remark))); } 
        }
        
        private DateTime _date = DateTime.Now;
        public DateTime Date 
        { 
            get => _date; 
            set { _date = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Date))); } 
        }
    }

    public class ClassEvaluationViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private readonly string _baseDir;

        public ObservableCollection<string> Classes { get; } = new();
        public ObservableCollection<string> Items { get; } = new();
        public ObservableCollection<ClassEvaluationRecord> Records { get; } = new();

        private string? _selectedClass;
        public string? SelectedClass { get => _selectedClass; set { _selectedClass = value; PropertyChanged?.Invoke(this, new(nameof(SelectedClass))); LoadItemsForClass(); } }

        private string? _selectedItem;
        public string? SelectedItem { get => _selectedItem; set { _selectedItem = value; PropertyChanged?.Invoke(this, new(nameof(SelectedItem))); } }

        private string? _studentName;
        public string? StudentName { get => _studentName; set { _studentName = value; PropertyChanged?.Invoke(this, new(nameof(StudentName))); } }

        private string? _remark;
        public string? Remark { get => _remark; set { _remark = value; PropertyChanged?.Invoke(this, new(nameof(Remark))); } }

        private string? _scoreText;
        public string? ScoreText { get => _scoreText; set { _scoreText = value; PropertyChanged?.Invoke(this, new(nameof(ScoreText))); } }

        public ICommand AddCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ResetCommand { get; }

        public ClassEvaluationViewModel(string? baseDir = null)
        {
            _baseDir = string.IsNullOrEmpty(baseDir) ? Path.Combine(Directory.GetCurrentDirectory(), "base") : baseDir;
            AddCommand = new RelayCommand(_ => { if (!ScoreManagerForSchool.UI.Security.AuthManager.Ensure(_baseDir)) return; Add(); });
            SaveCommand = new RelayCommand(_ => { if (!ScoreManagerForSchool.UI.Security.AuthManager.Ensure(_baseDir)) return; Save(); });
            ResetCommand = new RelayCommand(_ => { if (!ScoreManagerForSchool.UI.Security.AuthManager.Ensure(_baseDir)) return; Reset(); });
            LoadClasses();
            LoadExisting();
        }

        private void LoadClasses()
        {
            Classes.Clear();
            var list = new ClassStore(_baseDir).Load();
            foreach (var c in list.Select(c => c.Class).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct()!)
                Classes.Add(c!);
        }

        private void LoadItemsForClass()
        {
            Items.Clear();
            var scheme = new SchemeStore(_baseDir).Load().FirstOrDefault();
            if (scheme == null || scheme.Length == 0)
            {
                // 没有方案时提示用户去配置
                Items.Add("请先在\"班级及评价方案管理\"设置方案");
                return;
            }
            foreach (var col in scheme.Where(s => !string.IsNullOrWhiteSpace(s)))
                Items.Add(col);
        }

        private void LoadExisting()
        {
            Records.Clear();
            var estore = new EvaluationStore(_baseDir);
            foreach (var r in estore.Load().Select(e => new ClassEvaluationRecord
            {
                Class = e.Class,
                Name = string.IsNullOrWhiteSpace(e.Name) ? e.StudentId : e.Name,
                Item = e.Item,
                Score = e.Score,
                Remark = e.Remark,
                Date = e.Date,
            }))
                Records.Add(r);
        }

        private void Add()
        {
            if (string.IsNullOrWhiteSpace(SelectedClass) || string.IsNullOrWhiteSpace(StudentName)) return;
            double score = 0;
            if (!string.IsNullOrWhiteSpace(ScoreText))
            {
                if (!double.TryParse(ScoreText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out score))
                    double.TryParse(ScoreText, out score);
            }
            Records.Add(new ClassEvaluationRecord
            {
                Class = SelectedClass,
                Name = StudentName,
                Item = SelectedItem,
                Score = score,
                Remark = Remark,
                Date = DateTime.Now,
            });
            // 清理快速继续
            Remark = string.Empty; ScoreText = string.Empty; StudentName = string.Empty;
            PropertyChanged?.Invoke(this, new(nameof(Remark))); PropertyChanged?.Invoke(this, new(nameof(ScoreText))); PropertyChanged?.Invoke(this, new(nameof(StudentName)));
        }

        private void Save()
        {
            var store = new EvaluationStore(_baseDir);
            var list = Records.Select(r => new EvaluationEntry
            {
                Class = r.Class,
                Name = r.Name,
                Item = r.Item,
                Score = r.Score,
                Remark = r.Remark,
                Date = r.Date,
            }).ToList();
            store.Save(list);
        }

        private void Reset()
        {
            Records.Clear();
            new EvaluationStore(_baseDir).Save(Enumerable.Empty<EvaluationEntry>());
        }
    }
}
