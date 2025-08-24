using Avalonia.Controls;
using Avalonia.Interactivity;
using ScoreManagerForSchool.UI.ViewModels;
using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using System.Threading.Tasks;
using Avalonia.Input;
using System.Collections.Generic;
using ScoreManagerForSchool.UI.Services;

namespace ScoreManagerForSchool.UI.Views;

public partial class OobeWindow : Window
{
    private readonly HashSet<Control> _revealed = new();
    public OobeWindow()
    {
        InitializeComponent();
        try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smfs_oobe.log"), DateTime.UtcNow.ToString("o") + " OobeWindow ctor\n"); } catch { }
        try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smfs_oobe.log"), DateTime.UtcNow.ToString("o") + " DataContext=" + (this.DataContext?.GetType().FullName ?? "null") + "\n"); } catch { }
        // Ensure a ViewModel is present so bindings (StudentsPath etc.) work even if caller didn't set DataContext
        if (this.DataContext == null)
        {
            try
            {
                this.DataContext = new ScoreManagerForSchool.UI.ViewModels.OobeViewModel();
                try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smfs_oobe.log"), DateTime.UtcNow.ToString("o") + " DataContext assigned new OobeViewModel\n"); } catch { }
            }
            catch { }
        }

        // Build password inputs at runtime for better cross-platform compatibility
        try
        {
            var host1 = this.FindControl<ContentControl>("UserPwdHost");
            var host2 = this.FindControl<ContentControl>("UserPwdConfirmHost");
            CreateRuntimePasswordPair(host1, "UserPwdBox");
            CreateRuntimePasswordPair(host2, "UserPwdConfirmBox");

            var t1 = this.FindControl<ToggleButton>("UserPwdToggle");
            if (t1 != null && host1 != null)
            {
                t1.Click += (_, __) => ToggleRevealHost(host1, "UserPwdBox");
            }
            var t2 = this.FindControl<ToggleButton>("UserPwdConfirmToggle");
            if (t2 != null && host2 != null)
            {
                t2.Click += (_, __) => ToggleRevealHost(host2, "UserPwdConfirmBox");
            }
        }
        catch { }
    }
    private void ToggleReveal(TextBox box)
    {
        try
        {
            // If Tag contains the real password use it; otherwise assume current Text is the real value.
            var real = box.Tag as string;
            if (real == null) real = box.Text ?? string.Empty;

            if (_revealed.Contains(box))
            {
                // currently revealed -> mask and keep real in Tag
                _revealed.Remove(box);
                box.Tag = real;
                box.IsReadOnly = true;
                box.Text = new string('●', real.Length);
            }
            else
            {
                // currently masked -> reveal using Tag/real
                _revealed.Add(box);
                box.IsReadOnly = false;
                box.Text = real;
            }
        }
        catch { }
    }

    private string GetPassword(string name, string plainName)
    {
        try
        {
            // name refers to runtime created PasswordBox host name
            ContentControl? ctrl = null;
            // try common host name patterns
            ctrl = this.FindControl<ContentControl>(name + "Host") ?? this.FindControl<ContentControl>(name);
            if (ctrl == null && name.EndsWith("Box", StringComparison.OrdinalIgnoreCase))
            {
                var alt = name.Substring(0, name.Length - 3); // remove 'Box'
                ctrl = this.FindControl<ContentControl>(alt + "Host") ?? this.FindControl<ContentControl>(alt);
            }
            // also try plainName as direct host
            if (ctrl == null) ctrl = this.FindControl<ContentControl>(plainName + "Host") ?? this.FindControl<ContentControl>(plainName);
            if (ctrl != null)
            {
                if (ctrl.Content != null)
                {
                    var secure = ctrl.Content.GetType().GetProperty("Password");
                    if (secure != null) return (string?)secure.GetValue(ctrl.Content) ?? string.Empty;
                    var tb = ctrl.Content as TextBox;
                    if (tb != null)
                    {
                        // Prefer Tag (real password) over Text (which may be masked)
                        var real = tb.Tag as string;
                        return real ?? (tb.Text ?? string.Empty);
                    }
                }
                // maybe host.Tag holds plain textbox
                var ttag = ctrl.Tag as TextBox;
                if (ttag != null)
                {
                    var real = ttag.Tag as string;
                    return real ?? (ttag.Text ?? string.Empty);
                }
            }
        }
        catch { }
        return string.Empty;
    }

    private void CreateRuntimePasswordPair(ContentControl? host, string baseName)
    {
        try
        {
            if (host == null) return;
            // try to create Avalonia.Controls.PasswordBox via reflection
            var asm = typeof(ContentControl).Assembly; // Avalonia.Controls assembly
            // Try multiple ways to find PasswordBox type (some environments/versions differ)
            var pwdType = Type.GetType("Avalonia.Controls.PasswordBox, Avalonia.Controls") ?? asm.GetType("Avalonia.Controls.PasswordBox") ?? asm.GetType("Avalonia.Controls.Primitives.PasswordBox");
            object? pwd = null;
            if (pwdType != null)
            {
                pwd = Activator.CreateInstance(pwdType);
                // set name via reflection
                var nameProp = pwdType.GetProperty("Name");
                if (nameProp != null) nameProp.SetValue(pwd, baseName);
            }
            // plain textbox
            var plain = new TextBox { Name = baseName + "Plain", IsVisible = pwd == null };
            // initialize Tag to hold the real password (empty initially)
            plain.Tag = string.Empty;
            // If no PasswordBox is available, implement masked behavior on the plain TextBox:
            if (pwd == null)
            {
                // show masked placeholder (empty initially)
                plain.IsReadOnly = false;
                // handle direct text input (typed characters)
                plain.TextInput += (_, e) =>
                {
                    try
                    {
                        if (_revealed.Contains(plain)) return;
                        var ch = e.Text ?? string.Empty;
                        var real = (string?)plain.Tag ?? string.Empty;
                        var pos = Math.Min(plain.SelectionStart, real.Length);
                        var sel = plain.SelectedText?.Length ?? 0;
                        if (sel > 0) real = real.Remove(pos, sel);
                        real = real.Insert(pos, ch);
                        plain.Tag = real;
                        plain.Text = new string('●', real.Length);
                        plain.SelectionStart = pos + ch.Length;
                        e.Handled = true;
                    }
                    catch { }
                };

                // handle special keys: backspace, delete, paste
                plain.KeyDown += (_, e) =>
                {
                    try
                    {
                        if (_revealed.Contains(plain)) return;
                        if (e.Key == Key.Back)
                        {
                            var real = (string?)plain.Tag ?? string.Empty;
                            var pos = plain.SelectionStart;
                            var sel = plain.SelectedText?.Length ?? 0;
                            if (sel > 0)
                            {
                                real = real.Remove(pos, sel);
                                plain.Tag = real;
                                plain.Text = new string('●', real.Length);
                                plain.SelectionStart = pos;
                            }
                            else if (pos > 0)
                            {
                                real = real.Remove(pos - 1, 1);
                                plain.Tag = real;
                                plain.Text = new string('●', real.Length);
                                plain.SelectionStart = pos - 1;
                            }
                            e.Handled = true;
                        }
                        else if (e.Key == Key.Delete)
                        {
                            var real = (string?)plain.Tag ?? string.Empty;
                            var pos = plain.SelectionStart;
                            var sel = plain.SelectedText?.Length ?? 0;
                            if (sel > 0)
                            {
                                real = real.Remove(pos, sel);
                                plain.Tag = real;
                                plain.Text = new string('●', real.Length);
                                plain.SelectionStart = pos;
                            }
                            else if (pos < real.Length)
                            {
                                real = real.Remove(pos, 1);
                                plain.Tag = real;
                                plain.Text = new string('●', real.Length);
                                plain.SelectionStart = pos;
                            }
                            e.Handled = true;
                        }
                        // paste handling removed for now to avoid clipboard API differences across platforms
                    }
                    catch { }
                };
            }
            // If PasswordBox type wasn't available, plain will be used as fallback.
            // Wire events: when plain is visible and user edits while revealed, keep Tag in sync;
            // when plain loses focus and is not in revealed state, store real to Tag and mask the display.
            plain.TextChanged += (_, __) =>
            {
                try
                {
                    if (_revealed.Contains(plain))
                    {
                        plain.Tag = plain.Text ?? string.Empty;
                    }
                }
                catch { }
            };
            plain.LostFocus += (_, __) =>
            {
                try
                {
                    var real = plain.Text ?? string.Empty;
                    plain.Tag = real;
                    if (!_revealed.Contains(plain))
                    {
                        plain.Text = new string('●', real.Length);
                    }
                }
                catch { }
            };
            // host content: if password box available, put it and keep plain hidden; else use plain textbox visible
            host.Content = pwd ?? (object)plain;
            // store plain as Tag on host for fallback if needed
            host.Tag = plain;
        }
        catch { }
    }

    private void ToggleRevealHost(ContentControl host, string baseName)
    {
        try
        {
            if (host?.Content == null)
            {
                // fallback to plain TextBox in Tag
                var tb = host?.Tag as TextBox;
                if (tb != null) ToggleReveal(tb);
                return;
            }
            var content = host.Content;
            var pwdProp = content.GetType().GetProperty("Password");
            if (pwdProp != null)
            {
                // password box is present
                // find plain TextBox stored in Tag
                var plain = host.Tag as TextBox;
                if (plain == null)
                {
                    plain = new TextBox { Name = baseName + "Plain" };
                    host.Tag = plain;
                }
                // toggle visibility between password instance (content) and plain
                var isRevealed = _revealed.Contains(plain);
                if (isRevealed)
                {
                    // hide plain, copy back to password (use Tag if available to avoid losing characters)
                    try { pwdProp.SetValue(content, (string?)(plain.Tag as string) ?? plain.Text ?? string.Empty); } catch { }
                    plain.IsReadOnly = true;
                    _revealed.Remove(plain);
                    host.Content = content;
                }
                else
                {
                    // show plain and copy password into plain.Text and Tag
                    try { var val = (string?)pwdProp.GetValue(content) ?? string.Empty; plain.Text = val; plain.Tag = val; } catch { }
                    plain.IsReadOnly = false;
                    _revealed.Add(plain);
                    host.Content = plain;
                }
            }
            else
            {
                // no passwordbox type available; treat host.Content as TextBox
                var tb = host.Content as TextBox;
                if (tb != null)
                {
                    var isRevealed = _revealed.Contains(tb);
                    if (isRevealed)
                    {
                        // hide: persist real value then mask and make read-only
                        tb.Tag = tb.Text ?? tb.Tag as string ?? string.Empty;
                        tb.IsReadOnly = true;
                        tb.Text = new string('●', (tb.Tag as string)?.Length ?? 0);
                        _revealed.Remove(tb);
                    }
                    else
                    {
                        // reveal: restore real value from Tag and allow editing
                        tb.Text = tb.Tag as string ?? string.Empty;
                        tb.IsReadOnly = false;
                        _revealed.Add(tb);
                    }
                }
            }
        }
        catch { }
    }

    // old Text-based masking removed; PasswordBox used with plain TextBox overlay for reveal

    private void OnCancel(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.Close();
    }

    private async void OnFinish(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smfs_oobe.log"), DateTime.UtcNow.ToString("o") + " OnFinish invoked\n"); } catch { }
        if (this.DataContext is OobeViewModel vm)
        {
            string user = GetPassword("UserPwdBox", "UserPwdBoxPlain");
            string userConf = GetPassword("UserPwdConfirmBox", "UserPwdConfirmBoxPlain");

            // validation
            string? err = null;
            if (user.Length < 8) err = "用户密码长度应至少为8位。";
            else if (user != userConf) err = "用户密码与确认不匹配。";
            ;

            if (err != null)
            {
                var mt = this.FindControl<TextBlock>("MessageText");
                if (mt != null) mt.Text = err;
                // highlight offending boxes via reflection
                if (user.Length < 8) TrySetBackground("UserPwdBox", Colors.LightCoral);
                if (user != userConf) TrySetBackground("UserPwdConfirmBox", Colors.LightCoral);
                
                await ShowMessageAsync(this, "验证失败", err);
                try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smfs_oobe.log"), DateTime.UtcNow.ToString("o") + " Validation failed: " + err + "\n"); } catch { }
                return;
            }

            try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smfs_oobe.log"), DateTime.UtcNow.ToString("o") + " Calling SaveAndImport\n"); } catch { }
            var ok = false;
            try
            {
                ok = vm.SaveAndImport(user.AsSpan(), userConf.AsSpan());
            }
            catch (Exception ex)
            {
                try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smfs_oobe.log"), DateTime.UtcNow.ToString("o") + " SaveAndImport threw: " + ex.ToString() + "\n"); } catch { }
                ok = false;
            }
            try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smfs_oobe.log"), DateTime.UtcNow.ToString("o") + " SaveAndImport returned=" + ok + "\n"); } catch { }
            if (!ok)
            {
                var mt = this.FindControl<TextBlock>("MessageText");
                if (mt != null) mt.Text = "保存或导入失败，请检查 CSV 路径与格式。";
                await ShowMessageAsync(this, "错误", "保存或导入失败，请检查 CSV 路径与格式。");
                try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smfs_oobe.log"), DateTime.UtcNow.ToString("o") + " SaveAndImport failed - user notified\n"); } catch { }
                return;
            }

            // success: do not clear inputs per requirement
            try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smfs_oobe.log"), DateTime.UtcNow.ToString("o") + " SaveAndImport success - showing completion message\n"); } catch { }
            await ShowMessageAsync(this, "完成", "设置已保存。");
            try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smfs_oobe.log"), DateTime.UtcNow.ToString("o") + " Completion message closed\n"); } catch { }
            var app = Application.Current;
            if (app != null)
            {
                var lifetime = app.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                if (lifetime != null)
                {
                    try
                    {
                        // Show the main window first to ensure the app doesn't exit when OOBE closes
                        var main = new Views.MainWindow { DataContext = new ViewModels.MainWindowViewModel() };
                        main.Show();
                        lifetime.MainWindow = main;
                        // Now it's safe to close OOBE
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smfs_oobe.log"), DateTime.UtcNow.ToString("o") + " Transition to MainWindow failed: " + ex + "\n"); } catch { }
                        // Best-effort fallback: keep OOBE open with error
                        await ShowMessageAsync(this, "错误", "进入主界面失败：" + ex.Message);
                    }
                }
            }
        }
    }

    private async void OnBrowseStudents(object? sender, RoutedEventArgs e)
    {
        var path = await PickCsvFileAsync(this);
        if (!string.IsNullOrEmpty(path))
        {
            // 规范绝对路径
            try { if (Uri.TryCreate(path, UriKind.Absolute, out var u) && u.IsFile) path = u.LocalPath; path = System.IO.Path.GetFullPath(path); } catch { }
            try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smfs_oobe.log"), DateTime.UtcNow.ToString("o") + " Picked Students path=" + path + "\n"); } catch { }
            var tb = this.FindControl<TextBox>("StudentsPathBox");
            if (tb != null) tb.Text = path;
            try
            {
                if (this.DataContext is ScoreManagerForSchool.UI.ViewModels.OobeViewModel vm) vm.StudentsPath = path;
            }
            catch { }
        }
    }

    private async void OnBrowseClasses(object? sender, RoutedEventArgs e)
    {
        var path = await PickCsvFileAsync(this);
        if (!string.IsNullOrEmpty(path))
        {
            try { if (Uri.TryCreate(path, UriKind.Absolute, out var u) && u.IsFile) path = u.LocalPath; path = System.IO.Path.GetFullPath(path); } catch { }
            try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smfs_oobe.log"), DateTime.UtcNow.ToString("o") + " Picked Classes path=" + path + "\n"); } catch { }
            var tb = this.FindControl<TextBox>("ClassesPathBox");
            if (tb != null) tb.Text = path;
            try
            {
                if (this.DataContext is ScoreManagerForSchool.UI.ViewModels.OobeViewModel vm) vm.ClassesPath = path;
            }
            catch { }
        }
    }

    private async void OnBrowseSchemes(object? sender, RoutedEventArgs e)
    {
        var path = await PickCsvFileAsync(this);
        if (!string.IsNullOrEmpty(path))
        {
            try { if (Uri.TryCreate(path, UriKind.Absolute, out var u) && u.IsFile) path = u.LocalPath; path = System.IO.Path.GetFullPath(path); } catch { }
            try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smfs_oobe.log"), DateTime.UtcNow.ToString("o") + " Picked Schemes path=" + path + "\n"); } catch { }
            var tb = this.FindControl<TextBox>("SchemesPathBox");
            if (tb != null) tb.Text = path;
            try
            {
                if (this.DataContext is ScoreManagerForSchool.UI.ViewModels.OobeViewModel vm) vm.SchemesPath = path;
            }
            catch { }
        }
    }

    private async void OnShowFormatInfo(object? sender, RoutedEventArgs e)
    {
        var msg = "支持的文件: CSV、XLS、XLSX\n\n" +
                  "学生名单列顺序: 班级, 唯一号, 姓名\n" +
                  "班级列表列顺序: 班级, 类型\n" +
                  "评价方案: 任意列，自由格式（可选表头）\n\n" +
                  "注意:\n- 勾选‘首行为表头’时将跳过第一行\n- CSV 请使用逗号分隔\n- Excel 多表会顺序读取所有工作表\n- 仅限扩展名 .csv/.xls/.xlsx";
        await ShowMessageAsync(this, "导入格式说明", msg);
    }

    private static async System.Threading.Tasks.Task<string?> PickCsvFileAsync(Window owner)
    {
        try
        {
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(owner);
            if (topLevel == null) return null;
            return await FilePickerUtil.PickCsvOrExcelToLocalPathAsync(topLevel, "选择 CSV/Excel 文件");
        }
        catch { return null; }
    }

    private void TrySetBackground(string controlName, Color c)
    {
        try
        {
            var ctrl = this.FindControl<Control>(controlName);
            if (ctrl == null) return;
            var bgProp = ctrl.GetType().GetProperty("Background");
            if (bgProp != null) bgProp.SetValue(ctrl, new SolidColorBrush(c));
        }
        catch { }
    }

    private async Task ShowMessageAsync(Window owner, string title, string message)
    {
        var root = new StackPanel { Margin = new Thickness(12) };
        root.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
        var ok = new Button { Content = "确定", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Margin = new Thickness(0,8,0,0) };
        root.Children.Add(ok);
        var dlg = new Window { Title = title, Width = 480, Height = 160, Content = root };
        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
        ok.Click += (_, __) => { try { dlg.Close(); } catch { } tcs.TrySetResult(true); };
        if (owner != null)
        {
            await dlg.ShowDialog(owner);
        }
        else
        {
            dlg.Show();
            // wait for OK click
            await tcs.Task.ConfigureAwait(false);
            try { dlg.Close(); } catch { }
        }
        // ensure any remaining wait completes
        try { await tcs.Task.ConfigureAwait(false); } catch { }
    }
}
