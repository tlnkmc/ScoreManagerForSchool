using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;
using System.IO;

namespace ScoreManagerForSchool.UI.Views
{
    public partial class AboutView : UserControl
    {
        public AboutView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnOpenReleasesFolder(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                var root = AppContext.BaseDirectory; // 运行目录
                // 假设运行于仓库编译环境：releases 位于解决方案根目录下
                // 发布产物实际分发时，可改为当前目录或相对路径提示
                var probe = Path.GetFullPath(Path.Combine(root, "..", "..", "..", "..", "releases"));
                var path = Directory.Exists(probe) ? probe : Path.Combine(root, "releases");
                if (!Directory.Exists(path))
                {
                    // 兜底：打开运行目录
                    path = root;
                }

                OpenInFileManager(path);
            }
            catch (Exception ex)
            {
                // 简单兜底：忽略或弹窗（如需）
                Debug.WriteLine(ex);
            }
        }

        private static void OpenInFileManager(string path)
        {
            try
            {
#if WINDOWS
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{path}\"",
                    UseShellExecute = false
                });
#elif MACOS
                Process.Start("open", path);
#elif LINUX
                Process.Start("xdg-open", path);
#else
                // 未知平台，尝试 ShellExecute
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
