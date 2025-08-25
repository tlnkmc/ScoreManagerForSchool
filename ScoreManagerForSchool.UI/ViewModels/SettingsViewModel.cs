using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using ScoreManagerForSchool.Core.Storage;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;

namespace ScoreManagerForSchool.UI.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly string _baseDir;
        private readonly AppConfigStore _store;
        private AppConfig _cfg;
        
        // 委托用于更新亚克力效果
        public Action<bool>? UpdateAcrylicEffect { get; set; }

        public string[] ThemeOptions { get; } = new[] { "跟随系统", "浅色", "深色" };

        private string _theme;
        public string Theme 
        { 
            get => _theme; 
            set 
            { 
                if (_theme != value) 
                { 
                    _theme = value; 
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Theme)));
                    // 立即应用主题
                    ApplyThemeImmediately(value);
                } 
            } 
        }

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

    private bool _enableAcrylicEffect;
    public bool EnableAcrylicEffect 
    { 
        get => _enableAcrylicEffect; 
        set 
        { 
            if (_enableAcrylicEffect != value) 
            { 
                _enableAcrylicEffect = value; 
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnableAcrylicEffect)));
                // 立即保存配置并应用亚克力效果
                SaveAcrylicEffectImmediately(value);
            } 
        } 
    }

    // 密码修改相关属性
    private string _currentUserPassword = "";
    public string CurrentUserPassword { get => _currentUserPassword; set { if (_currentUserPassword != value) { _currentUserPassword = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentUserPassword))); } } }

    private string _newUserPassword = "";
    public string NewUserPassword { get => _newUserPassword; set { if (_newUserPassword != value) { _newUserPassword = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NewUserPassword))); } } }

    private string _newUserPasswordConfirm = "";
    public string NewUserPasswordConfirm { get => _newUserPasswordConfirm; set { if (_newUserPasswordConfirm != value) { _newUserPasswordConfirm = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NewUserPasswordConfirm))); } } }

        public SettingsViewModel(string? baseDir = null)
        {
            _baseDir = string.IsNullOrEmpty(baseDir) ? Path.Combine(Directory.GetCurrentDirectory(), "base") : baseDir;
            _store = new AppConfigStore(_baseDir);
            _cfg = _store.Load();
            // 将英文主题转换为中文显示
            _theme = _cfg.Theme switch
            {
                "Light" => "浅色",
                "Dark" => "深色",
                _ => "跟随系统"
            };
            _studentsCsvHeaderDefault = _cfg.StudentsCsvHeaderDefault;
            _autoStart = _cfg.AutoStart;
            _autoUpdateCheck = _cfg.AutoUpdateCheck;
            _updateFeedUrl = _cfg.UpdateFeedUrl;
            _updateSource = _cfg.UpdateSource;
            _themeAccent = _cfg.ThemeAccent;
            _enableAcrylicEffect = _cfg.EnableAcrylicEffect;

            SaveCommand = new RelayCommand(_ => Save());
            ResetFilesCommand = new RelayCommand(_ => Database1Store.DeleteBaseFiles(_baseDir));
            CheckUpdateCommand = new RelayCommand(async _ => await CheckUpdateAsync());
            ChangePasswordCommand = new RelayCommand(_ => ChangePassword());
            // 订阅属性变化，进行防抖自动保存
            PropertyChanged += OnAnyPropertyChanged;
        }

        private System.Threading.CancellationTokenSource? _saveCts;
        private void OnAnyPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 忽略仅供输入的密码字段
            if (e.PropertyName == nameof(CurrentUserPassword) || e.PropertyName == nameof(NewUserPassword) || e.PropertyName == nameof(NewUserPasswordConfirm))
                return;
            // 300ms 防抖合并保存
            try { _saveCts?.Cancel(); } catch { }
            _saveCts = new System.Threading.CancellationTokenSource();
            var ct = _saveCts.Token;
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    await System.Threading.Tasks.Task.Delay(300, ct);
                    if (ct.IsCancellationRequested) return;
                    Save();
                }
                catch (System.OperationCanceledException) { }
                catch { }
            });
        }

        private void Save()
        {
            // 将中文主题转换为英文主题代码保存
            var englishTheme = Theme switch
            {
                "浅色" => "Light",
                "深色" => "Dark",
                _ => "System"
            };
            
            _cfg.Theme = englishTheme;
            _cfg.StudentsCsvHeaderDefault = StudentsCsvHeaderDefault;
            _cfg.AutoStart = AutoStart;
            _cfg.AutoUpdateCheck = AutoUpdateCheck;
            _cfg.UpdateFeedUrl = UpdateFeedUrl;
            _cfg.UpdateSource = UpdateSource;
            _cfg.ThemeAccent = ThemeAccent;
            _cfg.EnableAcrylicEffect = EnableAcrylicEffect;
            _store.Save(_cfg);
            try
            {
                // 确保在 UI 线程上执行主题变更
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        Application.Current!.RequestedThemeVariant = englishTheme switch
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
                });
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
            
            // Apply acrylic effect
            try
            {
                // 确保在 UI 线程上执行亚克力效果更新
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        UpdateAcrylicEffect?.Invoke(_cfg.EnableAcrylicEffect);
                    }
                    catch { }
                });
            }
            catch { }
        }

        private async System.Threading.Tasks.Task CheckUpdateAsync()
        {
            try
            {
                // 当非 Custom 源时，我们自动构造 ver.txt 地址
                string? feed = UpdateFeedUrl;
                if (!string.Equals(UpdateSource, "Custom", StringComparison.OrdinalIgnoreCase))
                {
                    // 固定仓库路径：github/tlnkmc/ScoreManagerForSchool 的 ver.txt
                    var raw = "https://raw.githubusercontent.com/tlnkmc/ScoreManagerForSchool/main/releases/ver.txt";
                    feed = UI.Services.Updater.BuildSourceUrl(raw, UpdateSource);
                }
                
                if (string.IsNullOrWhiteSpace(feed))
                {
                    await ShowUpdateResultDialogAsync("配置错误", "更新源配置不正确");
                    return;
                }

                var info = await UI.Services.Updater.CheckAsync(feed!);
                if (info == null || string.IsNullOrWhiteSpace(info.Version))
                {
                    await ShowUpdateResultDialogAsync("检查失败", "无法获取更新信息，请检查网络连接或更新源配置");
                    return;
                }
                
                var current = UI.Services.Updater.GetCurrentVersion();
                if (!UI.Services.Updater.IsNewer(current, info.Version!))
                {
                    await ShowUpdateResultDialogAsync("已是最新版本", $"当前版本：{current}\n最新版本：{info.Version}\n\n您使用的已经是最新版本");
                    return;
                }

                // 有新版本，询问是否下载更新
                var shouldUpdate = await ShowUpdateAvailableDialogAsync(current, info);
                if (!shouldUpdate) return;

                // 下载并准备更新包
                var pkg = await UI.Services.Updater.DownloadAndPrepareUpdateAsync(info, UpdateSource, _baseDir);
                if (string.IsNullOrWhiteSpace(pkg))
                {
                    await ShowUpdateResultDialogAsync("下载失败", "下载更新包失败，请稍后重试");
                    return;
                }

                // 启动更新器
                UI.Services.Updater.StartUpdaterU(_baseDir);
            }
            catch (System.Exception ex)
            {
                await ShowUpdateResultDialogAsync("检查失败", $"更新检查时发生错误：{ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task ShowUpdateResultDialogAsync(string title, string message)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var window = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                    ? desktop.MainWindow 
                    : null;
                
                if (window != null)
                {
                    var dialog = new Avalonia.Controls.Window
                    {
                        Title = title,
                        Width = 400,
                        Height = 200,
                        WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
                        CanResize = false
                    };

                    var panel = new Avalonia.Controls.StackPanel
                    {
                        Margin = new Avalonia.Thickness(20),
                        Spacing = 15
                    };

                    panel.Children.Add(new Avalonia.Controls.TextBlock
                    {
                        Text = message,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        FontSize = 14
                    });

                    var buttonPanel = new Avalonia.Controls.StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Spacing = 10
                    };

                    var okButton = new Avalonia.Controls.Button
                    {
                        Content = "确定",
                        Width = 80,
                        IsDefault = true
                    };
                    okButton.Click += (_, __) => dialog.Close();

                    buttonPanel.Children.Add(okButton);
                    panel.Children.Add(buttonPanel);
                    dialog.Content = panel;

                    await dialog.ShowDialog(window);
                }
            });
        }

        private async System.Threading.Tasks.Task<bool> ShowUpdateAvailableDialogAsync(string currentVersion, UI.Services.UpdateInfo info)
        {
            return await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var window = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                    ? desktop.MainWindow 
                    : null;
                
                if (window == null) return false;

                var dialog = new Avalonia.Controls.Window
                {
                    Title = "发现新版本",
                    Width = 500,
                    Height = 300,
                    WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
                    CanResize = false
                };

                var panel = new Avalonia.Controls.StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 15
                };

                panel.Children.Add(new Avalonia.Controls.TextBlock
                {
                    Text = "发现新版本",
                    FontSize = 18,
                    FontWeight = Avalonia.Media.FontWeight.Bold
                });

                panel.Children.Add(new Avalonia.Controls.TextBlock
                {
                    Text = $"当前版本：{currentVersion}\n最新版本：{info.Version}",
                    FontSize = 14
                });

                if (!string.IsNullOrWhiteSpace(info.Notes))
                {
                    var notesContainer = new Avalonia.Controls.Border
                    {
                        Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(245, 245, 245)),
                        CornerRadius = new Avalonia.CornerRadius(4),
                        Padding = new Avalonia.Thickness(10),
                        MaxHeight = 120
                    };

                    var scrollViewer = new Avalonia.Controls.ScrollViewer();
                    scrollViewer.Content = new Avalonia.Controls.TextBlock
                    {
                        Text = info.Notes,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        FontSize = 12
                    };

                    notesContainer.Child = scrollViewer;
                    panel.Children.Add(notesContainer);
                }

                var buttonPanel = new Avalonia.Controls.StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 10
                };

                var updateButton = new Avalonia.Controls.Button
                {
                    Content = "立即更新",
                    Width = 80,
                    IsDefault = true
                };

                var cancelButton = new Avalonia.Controls.Button
                {
                    Content = "取消",
                    Width = 80
                };

                bool result = false;
                updateButton.Click += (_, __) => { result = true; dialog.Close(); };
                cancelButton.Click += (_, __) => { result = false; dialog.Close(); };

                buttonPanel.Children.Add(cancelButton);
                buttonPanel.Children.Add(updateButton);
                panel.Children.Add(buttonPanel);
                dialog.Content = panel;

                await dialog.ShowDialog(window);
                return result;
            });
        }

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

        private void ApplyThemeImmediately(string chineseTheme)
        {
            try
            {
                // 将中文主题转换为英文主题代码
                var englishTheme = chineseTheme switch
                {
                    "浅色" => "Light",
                    "深色" => "Dark",
                    _ => "System"
                };

                Application.Current!.RequestedThemeVariant = englishTheme switch
                {
                    "Light" => ThemeVariant.Light,
                    "Dark" => ThemeVariant.Dark,
                    _ => ThemeVariant.Default
                };
            }
            catch { }
        }

        private void SaveAcrylicEffectImmediately(bool enable)
        {
            try
            {
                // 更新配置并立即保存
                _cfg.EnableAcrylicEffect = enable;
                _store.Save(_cfg);
                
                // 确保在 UI 线程上执行亚克力效果更新
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        UpdateAcrylicEffect?.Invoke(enable);
                    }
                    catch { }
                });
            }
            catch { }
        }
    }
}
