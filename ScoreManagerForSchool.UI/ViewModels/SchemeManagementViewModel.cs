using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using ScoreManagerForSchool.Core.Storage;

namespace ScoreManagerForSchool.UI.ViewModels
{
    public class SchemeManagementViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly string _baseDir;
        private readonly ClassStore _classStore;
        private readonly SchemeStore _schemeStore;

        public ObservableCollection<ClassInfo> Classes { get; } = new();
        public ObservableCollection<string[]> Schemes { get; } = new();

        private bool _isDirty;
        public bool IsDirty { get => _isDirty; private set { _isDirty = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDirty))); } }

        public ICommand AddClassCommand { get; }
        public ICommand DeleteClassCommand { get; }
        public ICommand AddSchemeCommand { get; }
        public ICommand DeleteSchemeCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DiscardCommand { get; }
    public ICommand ImportClassesCommand { get; }
    public ICommand ImportSchemesCommand { get; }

        public SchemeManagementViewModel(string? baseDir = null)
        {
            _baseDir = string.IsNullOrEmpty(baseDir) ? Path.Combine(Directory.GetCurrentDirectory(), "base") : baseDir;
            _classStore = new ClassStore(_baseDir);
            _schemeStore = new SchemeStore(_baseDir);

            AddClassCommand = new RelayCommand(_ => { Classes.Add(new ClassInfo()); IsDirty = true; });
            DeleteClassCommand = new RelayCommand(p => { if (p is ClassInfo c) { Classes.Remove(c); IsDirty = true; } });
            AddSchemeCommand = new RelayCommand(_ => { Schemes.Add(new[] { "", "", "", "", "" }); IsDirty = true; });
            DeleteSchemeCommand = new RelayCommand(p => { if (p is string[] s) { Schemes.Remove(s); IsDirty = true; } });
            SaveCommand = new RelayCommand(_ => { if (!ScoreManagerForSchool.UI.Security.AuthManager.IsAuthenticated) { ScoreManagerForSchool.UI.Security.AuthManager.ShowLogin(); if (!ScoreManagerForSchool.UI.Security.AuthManager.IsAuthenticated) return; } Save(); });
            DiscardCommand = new RelayCommand(_ => Load());
            ImportClassesCommand = new RelayCommand(p => ImportClasses(p as string, true));
            ImportSchemesCommand = new RelayCommand(p => ImportSchemes(p as string, true));

            Load();
        }

        public void Load()
        {
            Classes.Clear();
            foreach (var c in _classStore.Load())
                Classes.Add(c);

            Schemes.Clear();
            foreach (var s in _schemeStore.Load())
                Schemes.Add(s);

            IsDirty = false;
        }

        public void Save()
        {
            _classStore.Save(Classes.ToList());
            _schemeStore.Save(Schemes.ToList());
            IsDirty = false;
        }

        // mark dirty on property change of items (simple approach: expose a method for view to call on TextBox changes)
        public void MarkDirty() => IsDirty = true;

        public void ImportClasses(string? path, bool firstRowIsHeader)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            var list = CsvImporter.ImportClasses(path, firstRowIsHeader);
            Classes.Clear();
            foreach (var c in list) Classes.Add(c);
            IsDirty = true;
        }

        public void ImportSchemes(string? path, bool firstRowIsHeader)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            var list = CsvImporter.ImportScheme(path, firstRowIsHeader);
            Schemes.Clear();
            foreach (var s in list) Schemes.Add(s);
            IsDirty = true;
        }
    }
}
