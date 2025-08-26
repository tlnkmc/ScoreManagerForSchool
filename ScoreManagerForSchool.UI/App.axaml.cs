using Avalonia;
using System.IO;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
// Removed duplicate usings
using ScoreManagerForSchool.UI.ViewModels;
using ScoreManagerForSchool.UI.Views;
using ScoreManagerForSchool.Core.Logging;
using ScoreManagerForSchool.Core.Storage;
using Avalonia.Styling;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using ScoreManagerForSchool.UI.Services;
using System;
// duplicate usings removed

namespace ScoreManagerForSchool.UI;

public partial class App : Application
{
    private static bool _updateCheckInProgress = false;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            HookGlobalExceptionHandlers(desktop);
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            // 如果 base/Database1.json 不存在或不完整，启动 OOBE
            var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "base");
            var dbPath = Path.Combine(baseDir, "Database1.json");
            bool needOobe = true;
            try
            {
                if (File.Exists(dbPath))
                {
                    var txt = File.ReadAllText(dbPath);
                    if (!string.IsNullOrWhiteSpace(txt) && txt.Contains("ID1")) needOobe = false;
                }
            }
            catch { }

            if (needOobe)
            {
                desktop.MainWindow = new Views.OobeWindow { DataContext = new OobeViewModel() };
            }
            else
            {
                // show login window as initial MainWindow
                var loginWin = new Views.LoginWindow();
                var vm = loginWin.DataContext as LoginViewModel ?? new LoginViewModel();
                loginWin.DataContext = vm;
                vm.CloseAction = ok =>
                {
                    try
                    {
                        if (ok)
                        {
                            ScoreManagerForSchool.UI.Security.AuthManager.IsAuthenticated = true;
                            var lifetime2 = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                            var main = new MainWindow { DataContext = new MainWindowViewModel() };
                            if (lifetime2 != null)
                            {
                                lifetime2.MainWindow = main;
                                main.Show();
                                TryAutoCheckUpdateAsync(main, baseDir);
                            }
                        }
                        else
                        {
                            (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
                        }
                    }
                    catch { }
                    finally { try { loginWin.Close(); } catch { } }
                };
                desktop.MainWindow = loginWin;
            }

            // Apply theme from config
            try
            {
                var cfg = new AppConfigStore(baseDir).Load();
                Application.Current!.RequestedThemeVariant = cfg.Theme switch
                {
                    "Light" => ThemeVariant.Light,
                    "Dark" => ThemeVariant.Dark,
                    _ => ThemeVariant.Default
                };

                if (!string.IsNullOrWhiteSpace(cfg.ThemeAccent))
                {
                    try
                    {
                        var color = Avalonia.Media.Color.Parse(cfg.ThemeAccent);
                        var res = Application.Current!.Resources;
                        res["SystemAccentColor"] = color;
                        res["AccentColor"] = color;
                    }
                    catch { }
                }
            }
            catch { }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void HookGlobalExceptionHandlers(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                LogAndShowGlobalError(desktop, ex ?? new Exception("Unknown unhandled exception."));
            };
        }
        catch { }
        try
        {
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                e.SetObserved();
                LogAndShowGlobalError(desktop, e.Exception);
            };
        }
        catch { }
    }

    private void LogAndShowGlobalError(IClassicDesktopStyleApplicationLifetime desktop, Exception ex)
    {
        try
        {
            var log = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smfs_fatal.log");
            System.IO.File.AppendAllText(log, DateTime.UtcNow.ToString("o") + " " + ex + Environment.NewLine);
        }
        catch { }

        try
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    var owner = desktop?.MainWindow;
                    var panel = new StackPanel { Margin = new Thickness(12), Spacing = 8 };
                    panel.Children.Add(new TextBlock { Text = "发生未处理异常：" + ex.Message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
                    var ok = new Button { Content = "确定", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
                    panel.Children.Add(ok);
                    var dlg = new Window { Title = "错误", Width = 520, Height = 220, Content = panel, WindowStartupLocation = WindowStartupLocation.CenterOwner };
                    ok.Click += (_, __) => { try { dlg.Close(); } catch { } };
                    if (owner != null)
                        await dlg.ShowDialog(owner);
                    else
                        dlg.Show();
                }
                catch { }
            });
        }
        catch { }
    }

    private async void TryAutoCheckUpdateAsync(Window owner, string baseDir)
    {
        // 防止重复检查
        if (_updateCheckInProgress)
        {
            Logger.LogInfo("Update check already in progress, skipping duplicate request", "App");
            return;
        }

        _updateCheckInProgress = true;
        
        try
        {
            var cfg = new AppConfigStore(baseDir).Load();
            // 启动时先删除程序目录内 update 文件夹（如有）
            try
            {
                var updDir = Path.Combine(AppContext.BaseDirectory, "update");
                if (Directory.Exists(updDir)) { Directory.Delete(updDir, true); }
            }
            catch { }

            // 若存在 update 包且允许自动更新，则启动 U1 流程；否则弹窗询问
            var hasUpdatePkg = File.Exists(Path.Combine(AppContext.BaseDirectory, "update.zip")) || File.Exists(Path.Combine(AppContext.BaseDirectory, "update.tar.gz")) || File.Exists(Path.Combine(AppContext.BaseDirectory, "update"));
            if (hasUpdatePkg)
            {
                if (cfg.AutoUpdateCheck)
                {
                    Updater.StartUpdaterU(AppContext.BaseDirectory);
                    return;
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        var p = new StackPanel { Margin = new Thickness(12), Spacing = 8 };
                        p.Children.Add(new TextBlock { Text = "检测到更新包，是否立即更新？", TextWrapping = Avalonia.Media.TextWrapping.Wrap });
                        var btns = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };
                        var btnYes = new Button { Content = "是" };
                        var btnNo = new Button { Content = "否" };
                        btns.Children.Add(btnYes);
                        btns.Children.Add(btnNo);
                        p.Children.Add(btns);
                        var dlg = new Window { Title = "更新", Width = 420, Height = 180, Content = p, WindowStartupLocation = WindowStartupLocation.CenterOwner };
                        btnYes.Click += (_, __) => { try { dlg.Close(true); } catch { } };
                        btnNo.Click += (_, __) => { try { dlg.Close(false); } catch { } };
                        var result = await dlg.ShowDialog<bool?>(owner);
                        if (result == true)
                        {
                            Updater.StartUpdaterU(AppContext.BaseDirectory);
                            return;
                        }
                    });
                }
            }

            if (!cfg.AutoUpdateCheck) return;

            // 构造 ver.txt 源地址
            string? feed = cfg.UpdateFeedUrl;
            if (!string.Equals(cfg.UpdateSource, "Custom", StringComparison.OrdinalIgnoreCase))
            {
                var raw = "https://raw.githubusercontent.com/tlnkmc/ScoreManagerForSchool/main/ver.txt";
                feed = Updater.BuildSourceUrl(raw, cfg.UpdateSource);
            }
            if (string.IsNullOrWhiteSpace(feed)) 
            {
                Logger.LogWarning("Background update check cancelled - Update source configuration is empty", "App");
                return;
            }

            Logger.LogInfo($"Starting background update check - Update source: {cfg.UpdateSource}, URL: {feed}", "App");
            var info = await Updater.CheckAsync(feed!).ConfigureAwait(false);
            if (info == null || string.IsNullOrWhiteSpace(info.Version)) 
            {
                Logger.LogWarning("Background update check failed - No valid version info received", "App");
                return;
            }

            var current = Updater.GetCurrentVersion();
            bool newer = Updater.IsNewer(current, info.Version!);
            Logger.LogInfo($"Background update check completed - Current version: {current}, Remote version: {info.Version}, Has new version: {newer}", "App");

            if (!newer) return;

            Logger.LogInfo("New version found, starting background update package download", "App");
            // 后台下载到程序目录，并准备 update 包
            var pkg = await Updater.DownloadAndPrepareUpdateAsync(info, cfg.UpdateSource, AppContext.BaseDirectory).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(pkg)) 
            {
                Logger.LogError("Background update package download failed", "App");
                return;
            }

            Logger.LogInfo($"Background update package download successful: {pkg}", "App");
            Logger.LogInfo("Preparing to show update notification window", "App");

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var win = new Views.UpdateAvailableWindow { DataContext = info };
                win.Show(owner);
                Logger.LogInfo("Update notification window displayed", "App");
            });
        }
        catch { }
        finally
        {
            _updateCheckInProgress = false;
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}