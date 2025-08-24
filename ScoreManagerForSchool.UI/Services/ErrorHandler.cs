using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using ScoreManagerForSchool.Core.Logging;

namespace ScoreManagerForSchool.UI.Services
{
    public static class ErrorHandler
    {
        public static void HandleError(Exception exception, string? userMessage = null, string? source = null)
        {
            var errorMessage = userMessage ?? "发生了未知错误";
            
            // 记录日志
            Logger.LogError($"错误处理: {errorMessage}", source, exception);

            // 显示错误弹窗
            Task.Run(async () =>
            {
                try
                {
                    await ShowErrorDialog(errorMessage, exception);
                }
                catch (Exception dialogEx)
                {
                    Logger.LogError("显示错误对话框失败", "ErrorHandler", dialogEx);
                }
            });
        }

        public static void HandleError(string message, string? source = null)
        {
            Logger.LogError(message, source);
            
            Task.Run(async () =>
            {
                try
                {
                    await ShowErrorDialog(message);
                }
                catch (Exception dialogEx)
                {
                    Logger.LogError("显示错误对话框失败", "ErrorHandler", dialogEx);
                }
            });
        }

        public static async Task<bool> HandleErrorAsync(Exception exception, string? userMessage = null, string? source = null)
        {
            var errorMessage = userMessage ?? "发生了未知错误";
            
            Logger.LogError($"异步错误处理: {errorMessage}", source, exception);

            try
            {
                await ShowErrorDialog(errorMessage, exception);
                return true;
            }
            catch (Exception dialogEx)
            {
                Logger.LogError("显示错误对话框失败", "ErrorHandler", dialogEx);
                return false;
            }
        }

        private static async Task ShowErrorDialog(string message, Exception? exception = null)
        {
            try
            {
                var window = GetTopLevelWindow();
                if (window == null)
                {
                    Logger.LogWarning("无法找到顶级窗口，跳过错误对话框显示", "ErrorHandler");
                    return;
                }

                var dialog = new Window
                {
                    Title = "错误",
                    Width = 500,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false
                };

                var content = CreateErrorDialogContent(message, exception);
                dialog.Content = content;

                await dialog.ShowDialog(window);
            }
            catch (Exception ex)
            {
                Logger.LogError("创建错误对话框失败", "ErrorHandler", ex);
            }
        }

        private static Control CreateErrorDialogContent(string message, Exception? exception)
        {
            var mainPanel = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Spacing = 12
            };

            // 错误图标和标题
            var titlePanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 8
            };

            var errorIcon = new TextBlock
            {
                Text = "⚠️",
                FontSize = 24,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var titleText = new TextBlock
            {
                Text = "发生错误",
                FontSize = 16,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            titlePanel.Children.Add(errorIcon);
            titlePanel.Children.Add(titleText);
            mainPanel.Children.Add(titlePanel);

            // 错误消息
            var messageText = new TextBlock
            {
                Text = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(0, 8, 0, 0)
            };
            mainPanel.Children.Add(messageText);

            // 详细错误信息（可折叠）
            if (exception != null)
            {
                var detailsExpander = new Expander
                {
                    Header = "详细信息",
                    Margin = new Avalonia.Thickness(0, 8, 0, 0)
                };

                var detailsText = new TextBox
                {
                    Text = exception.ToString(),
                    IsReadOnly = true,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Height = 100
                };
                
                // 设置滚动条属性
                Avalonia.Controls.ScrollViewer.SetVerticalScrollBarVisibility(detailsText, Avalonia.Controls.Primitives.ScrollBarVisibility.Auto);

                detailsExpander.Content = detailsText;
                mainPanel.Children.Add(detailsExpander);
            }

            // 日志路径信息
            var logPath = Logger.GetCurrentLogPath();
            if (!string.IsNullOrEmpty(logPath))
            {
                var logInfo = new TextBlock
                {
                    Text = $"详细日志已保存至: {logPath}",
                    FontSize = 10,
                    Foreground = Avalonia.Media.Brushes.Gray,
                    Margin = new Avalonia.Thickness(0, 8, 0, 0),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                };
                mainPanel.Children.Add(logInfo);
            }

            // 按钮
            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 8,
                Margin = new Avalonia.Thickness(0, 16, 0, 0)
            };

            var okButton = new Button
            {
                Content = "确定",
                Width = 80,
                IsDefault = true
            };

            okButton.Click += (sender, e) =>
            {
                if (sender is Button btn)
                {
                    // 向上查找 Window
                    var current = btn.Parent;
                    while (current != null && !(current is Window))
                    {
                        current = current.Parent;
                    }
                    (current as Window)?.Close();
                }
            };

            buttonPanel.Children.Add(okButton);
            mainPanel.Children.Add(buttonPanel);

            return mainPanel;
        }

        private static Window? GetTopLevelWindow()
        {
            try
            {
                var app = Avalonia.Application.Current;
                if (app?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    return desktop.MainWindow;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
