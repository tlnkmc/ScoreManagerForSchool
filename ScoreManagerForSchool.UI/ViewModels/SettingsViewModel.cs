using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using ScoreManagerForSchool.Core.Storage;
using Avalonia;
using Avalonia.Styling;

namespace ScoreManagerForSchool.UI.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly string _baseDir;
        private readonly AppConfigStore _store;
        private AppConfig _cfg;

        public string[] ThemeOptions { get; } = new[] { "System", "Light", "Dark" };

        private string _theme;
        public string Theme { get => _theme; set { if (_theme != value) { _theme = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Theme))); } } }

        private bool _studentsCsvHeaderDefault;
        public bool StudentsCsvHeaderDefault { get => _studentsCsvHeaderDefault; set { if (_studentsCsvHeaderDefault != value) { _studentsCsvHeaderDefault = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StudentsCsvHeaderDefault))); } } }

    public ICommand SaveCommand { get; }
    public ICommand ResetFilesCommand { get; }
    public ICommand CheckUpdateCommand { get; }
    public ICommand ChangePasswordCommand { get; }

    private bool _autoStart;
    public bool AutoStart { get => _autoStart; set { if (_autoStart != value) { _autoStart = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoStart))); } } }

    private bool _autoUpdateCheck;
    public bool AutoUpdateCheck { get => _autoUpdateCheck; set { if (_autoUpdateCheck != value) { _autoUpdateCheck = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoUpdateCheck))); } } }

    private string? _updateFeedUrl;
    public string? UpdateFeedUrl { get => _updateFeedUrl; set { if (_updateFeedUrl != value) { _updateFeedUrl = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpdateFeedUrl))); } } }

    public string[] UpdateSourceOptions { get; } = new[] { "GitHub", "Ghproxy", "Custom" };
    private string _updateSource = "GitHub";
    public string UpdateSource { get => _updateSource; set { if (_updateSource != value) { _updateSource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpdateSource))); } } }

    private string? _themeAccent;
    public string? ThemeAccent { get => _themeAccent; set { if (_themeAccent != value) { _themeAccent = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ThemeAccent))); } } }

        public SettingsViewModel(string? baseDir = null)
        {
            _baseDir = string.IsNullOrEmpty(baseDir) ? Path.Combine(Directory.GetCurrentDirectory(), "base") : baseDir;
            _store = new AppConfigStore(_baseDir);
            _cfg = _store.Load();
            _theme = _cfg.Theme;
            _studentsCsvHeaderDefault = _cfg.StudentsCsvHeaderDefault;
            _autoStart = _cfg.AutoStart;
            _autoUpdateCheck = _cfg.AutoUpdateCheck;
            _updateFeedUrl = _cfg.UpdateFeedUrl;
            _updateSource = _cfg.UpdateSource;
            _themeAccent = _cfg.ThemeAccent;

            SaveCommand = new RelayCommand(_ => Save());
            ResetFilesCommand = new RelayCommand(_ => Database1Store.DeleteBaseFiles(_baseDir));
            CheckUpdateCommand = new RelayCommand(async _ => await CheckUpdateAsync());
            ChangePasswordCommand = new RelayCommand(_ => ChangePassword());
        }

        private void Save()
        {
            _cfg.Theme = Theme;
            _cfg.StudentsCsvHeaderDefault = StudentsCsvHeaderDefault;
            _cfg.AutoStart = AutoStart;
            _cfg.AutoUpdateCheck = AutoUpdateCheck;
            _cfg.UpdateFeedUrl = UpdateFeedUrl;
            _cfg.UpdateSource = UpdateSource;
            _cfg.ThemeAccent = ThemeAccent;
            _store.Save(_cfg);
            try
            {
                Application.Current!.RequestedThemeVariant = _cfg.Theme switch
                {
                    "Light" => ThemeVariant.Light,
                    "Dark" => ThemeVariant.Dark,
                    _ => ThemeVariant.Default
                };

                // Apply accent if provided (#RRGGBB)
                if (!string.IsNullOrWhiteSpace(_cfg.ThemeAccent))
                {
                    try
                    {
                        var color = Avalonia.Media.Color.Parse(_cfg.ThemeAccent);
                        var res = Application.Current!.Resources;
                        res["SystemAccentColor"] = color;
                        res["AccentColor"] = color;
                    }
                    catch { }
                }
            }
            catch { }

            // Apply autostart preference (Windows only)
            try
            {
                if (OperatingSystem.IsWindows() && UI.Services.AutostartManager.IsSupported)
                {
                    UI.Services.AutostartManager.SetEnabled(_cfg.AutoStart);
                }
            }
            catch { }
        }

        private async System.Threading.Tasks.Task CheckUpdateAsync()
        {
            // 当非 Custom 源时，我们自动构造 ver.txt 地址
            string? feed = UpdateFeedUrl;
            if (!string.Equals(UpdateSource, "Custom", StringComparison.OrdinalIgnoreCase))
            {
                // 固定仓库路径：github/tlnkmc/ScoreManagerForSchool 的 ver.txt
                var raw = "https://raw.githubusercontent.com/tlnkmc/ScoreManagerForSchool/main/releases/ver.txt";
                feed = UI.Services.Updater.BuildSourceUrl(raw, UpdateSource);
            }
            if (string.IsNullOrWhiteSpace(feed)) return;
            try
            {
                var info = await UI.Services.Updater.CheckAsync(feed!);
                if (info == null || string.IsNullOrWhiteSpace(info.Version)) return;
                var current = UI.Services.Updater.GetCurrentVersion();
                if (!UI.Services.Updater.IsNewer(current, info.Version!)) return;

                // 下载并准备更新包
                var pkg = await UI.Services.Updater.DownloadAndPrepareUpdateAsync(info, UpdateSource, _baseDir);
                if (string.IsNullOrWhiteSpace(pkg)) return;

                // 启动更新器 -u --token 并退出当前应用由更新器接管（此处仅启动更新器）
                UI.Services.Updater.StartUpdaterU(_baseDir);
            }
            catch { }
        }

        // Change password fields (simple binding; secure variants can be added similarly to OOBE)
    public string? CurrentUserPassword { get; set; }
    public string? NewUserPassword { get; set; }
    public string? NewUserPasswordConfirm { get; set; }

        private void ChangePassword()
        {
            try
            {
                var dbStore = new Database1Store(_baseDir);
                var db = dbStore.Load();
                if (db == null || string.IsNullOrEmpty(db.Salt1) || string.IsNullOrEmpty(db.ID1)) return;
                // Validate current user password
                if (string.IsNullOrEmpty(CurrentUserPassword)) return;
                var salt1 = Convert.FromBase64String(db.Salt1);
                var key1 = Core.Security.CryptoUtil.DeriveKey(CurrentUserPassword, salt1, 32, db.Iterations > 0 ? db.Iterations : 100000);
                var plain = Core.Security.CryptoUtil.DecryptFromBase64(db.ID1, key1);
                if (string.IsNullOrEmpty(plain) || !plain.StartsWith("0D0007211145141919810", StringComparison.Ordinal)) return;

                // Apply new user password if provided and valid
                var userPwd = CurrentUserPassword;
                if (!string.IsNullOrWhiteSpace(NewUserPassword))
                {
                    if (NewUserPassword != NewUserPasswordConfirm || NewUserPassword.Length < 8) return;
                    userPwd = NewUserPassword;
                }

                // Regenerate secrets (ID1 only)
                var rawSalt1 = Guid.NewGuid().ToByteArray();
                var keyNew1 = Core.Security.CryptoUtil.DeriveKey(userPwd, rawSalt1, 32, db.Iterations > 0 ? db.Iterations : 100000);
                var id1 = Core.Security.CryptoUtil.EncryptToBase64(BuildPayload().AsSpan(), keyNew1);
                db.ID1 = id1; db.Salt1 = Convert.ToBase64String(rawSalt1);
                dbStore.Save(db);
            }
            catch { }
        }

        private static string BuildPayload()
        {
            var bytes = new byte[118];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            var hex = BitConverter.ToString(bytes).Replace("-", string.Empty).ToUpperInvariant();
            if (hex.Length > 235) hex = hex.Substring(0, 235);
            return "0D0007211145141919810" + hex;
        }
    }
}
