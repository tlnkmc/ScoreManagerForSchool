using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using Avalonia.Controls;
using ScoreManagerForSchool.Core.Storage;
using ScoreManagerForSchool.Core.Security;

namespace ScoreManagerForSchool.UI.ViewModels
{
    public class OobeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _usersPath = string.Empty;
        public string StudentsPath { get => _usersPath; set { _usersPath = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StudentsPath))); } }

        private string _classesPath = string.Empty;
        public string ClassesPath { get => _classesPath; set { _classesPath = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ClassesPath))); } }

        private string _schemesPath = string.Empty;
        public string SchemesPath { get => _schemesPath; set { _schemesPath = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SchemesPath))); } }

        public ICommand ImportStudentsCommand { get; }
        public ICommand ImportClassesCommand { get; }
        public ICommand ImportSchemesCommand { get; }

    // Header flags for CSVs
    public bool StudentsHasHeader { get; set; } = true;
    public bool ClassesHasHeader { get; set; } = true;
    public bool SchemesHasHeader { get; set; } = true;

    public char[] UserPassword { get; set; } = Array.Empty<char>();
    public char[] UserPasswordConfirm { get; set; } = Array.Empty<char>();
    public string? UserHint { get; set; }
    // Security questions (two simple pairs)
    public string? Qa1Question { get; set; }
    public string? Qa1Answer { get; set; }
    public string? Qa2Question { get; set; }
    public string? Qa2Answer { get; set; }

    public ICommand CancelCommand { get; }
    public ICommand FinishCommand { get; }

        public OobeViewModel()
        {
        ImportStudentsCommand = new RelayCommand(_ =>
            {
                if (!string.IsNullOrEmpty(StudentsPath) && File.Exists(StudentsPath))
                {
            var students = CsvImporter.ImportStudents(StudentsPath, true);
                    var store = new StudentStore(Path.Combine(Directory.GetCurrentDirectory(), "base"));
                    store.Save(students);
                }
            });

        ImportClassesCommand = new RelayCommand(_ =>
            {
                if (!string.IsNullOrEmpty(ClassesPath) && File.Exists(ClassesPath))
                {
            var classes = CsvImporter.ImportClasses(ClassesPath, true);
                    var store = new ClassStore(Path.Combine(Directory.GetCurrentDirectory(), "base"));
                    store.Save(classes);
                }
            });

        ImportSchemesCommand = new RelayCommand(_ =>
            {
                if (!string.IsNullOrEmpty(SchemesPath) && File.Exists(SchemesPath))
                {
            var schemes = CsvImporter.ImportScheme(SchemesPath, true);
                    var store = new SchemeStore(Path.Combine(Directory.GetCurrentDirectory(), "base"));
                    store.Save(schemes);
                }
            });

            FinishCommand = new RelayCommand(_ => { });

            CancelCommand = new RelayCommand(_ => { });
        }

        // 可由窗口调用：在 OOBE 界面完成时持久化数据并导入 CSV，返回是否成功
    public bool SaveAndImport()
        {
            return SaveAndImport(new ReadOnlySpan<char>(UserPassword), new ReadOnlySpan<char>(UserPasswordConfirm));
        }

        // New overload: accept passwords as spans to avoid long-lived managed strings/ch buffers in VM
    public bool SaveAndImport(ReadOnlySpan<char> userPwd, ReadOnlySpan<char> userPwdConfirm)
        {
            if (userPwd.Length < 8) return false;
            if (userPwd.Length != userPwdConfirm.Length) return false;
            for (int i = 0; i < userPwd.Length; i++) if (userPwd[i] != userPwdConfirm[i]) return false;

            var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "base");
            Directory.CreateDirectory(baseDir);

            var randomHex = DeviceIdGenerator.GenerateDeviceId().ToUpper();
            var payload = "0D0007211145141919810" + randomHex;

            var rawSalt1 = Guid.NewGuid().ToByteArray();
            var salt1 = Convert.ToBase64String(rawSalt1);
            var key1 = CryptoUtil.DeriveKey(userPwd, rawSalt1, 32, 100000);
            var id1 = CryptoUtil.EncryptToBase64(payload.AsSpan(), key1);

            var model = new Database1Model { ID1 = id1, Salt1 = salt1, Iterations = 100000 };
            var store = new Database1Store(baseDir);
            store.Save(model);

            var hintStore = new PwhintStore(baseDir);
            hintStore.SaveHints(new string?[] { UserHint });

            // Save security questions
            if (string.IsNullOrWhiteSpace(Qa1Question) || string.IsNullOrWhiteSpace(Qa1Answer) || string.IsNullOrWhiteSpace(Qa2Question) || string.IsNullOrWhiteSpace(Qa2Answer))
                return false;
            var secPath = Path.Combine(baseDir, "secqa.json");
            var list = new System.Collections.Generic.List<object>();
            void AddQaLocal(string q, string a)
            {
                var salt = Guid.NewGuid().ToByteArray();
                var key = CryptoUtil.DeriveKey(a, salt, 32, 100000);
                list.Add(new { Question = q, Salt = Convert.ToBase64String(salt), Iterations = 100000, Hash = Convert.ToBase64String(key) });
            }
            AddQaLocal(Qa1Question!, Qa1Answer!);
            AddQaLocal(Qa2Question!, Qa2Answer!);
            var json = System.Text.Json.JsonSerializer.Serialize(new { Items = list }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(secPath, json);

            if (!string.IsNullOrEmpty(StudentsPath) && File.Exists(StudentsPath))
            {
                var students = CsvImporter.ImportStudents(StudentsPath, StudentsHasHeader);
                var sstore = new StudentStore(baseDir);
                sstore.Save(students);
            }
            if (!string.IsNullOrEmpty(ClassesPath) && File.Exists(ClassesPath))
            {
                var classes = CsvImporter.ImportClasses(ClassesPath, ClassesHasHeader);
                var cstore = new ClassStore(baseDir);
                cstore.Save(classes);
            }
            if (!string.IsNullOrEmpty(SchemesPath) && File.Exists(SchemesPath))
            {
                var schemes = CsvImporter.ImportScheme(SchemesPath, SchemesHasHeader);
                var sch = new SchemeStore(baseDir);
                sch.Save(schemes);
            }

            return true;
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _exec;
        public RelayCommand(Action<object?> exec) => _exec = exec;
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _exec(parameter);
    }
}
