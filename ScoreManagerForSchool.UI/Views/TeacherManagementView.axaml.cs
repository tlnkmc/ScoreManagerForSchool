using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ScoreManagerForSchool.UI.ViewModels;
using ScoreManagerForSchool.Core.Storage;
using System.Linq;
using System.Threading.Tasks;

namespace ScoreManagerForSchool.UI.Views
{
    public partial class TeacherManagementView : UserControl
    {
        public TeacherManagementView()
        {
            InitializeComponent();
            if (DataContext == null)
            {
                DataContext = new TeacherManagementViewModel();
            }

            // 设置文件对话框回调
            if (DataContext is TeacherManagementViewModel vm)
            {
                vm.SelectImportFileCallback = SelectImportFileAsync;
                vm.SelectExportFileCallback = SelectExportFileAsync;
            }
        }

        private async void OnManageSubjectGroups(object? sender, RoutedEventArgs e)
        {
            var dlg = new ScoreManagerForSchool.UI.Views.Dialogs.SubjectGroupManager();
            if (TopLevel.GetTopLevel(this) is Window parent)
                await dlg.ShowDialog(parent);
            if (DataContext is TeacherManagementViewModel vm)
            {
                vm.ReloadSubjectGroups();
            }
        }
        private async Task<string?> SelectImportFileAsync()
        {
            if (TopLevel.GetTopLevel(this) is not { } topLevel) return null;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择教师数据文件",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("CSV files") { Patterns = new[] { "*.csv" } },
                    new FilePickerFileType("All files") { Patterns = new[] { "*.*" } }
                }
            });

            return files.Count > 0 ? files[0].Path.LocalPath : null;
        }

        private async Task<string?> SelectExportFileAsync(string defaultFileName)
        {
            if (TopLevel.GetTopLevel(this) is not { } topLevel) return null;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "导出教师数据模板",
                SuggestedFileName = defaultFileName,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("CSV files") { Patterns = new[] { "*.csv" } }
                }
            });

            return file?.Path.LocalPath;
        }

        private void OnSaveTeacherClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is TeacherManagementViewModel vm)
            {
                vm.SaveCurrentTeacher();
            }
        }

        private async void OnAddTeacherClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new AddTeacherDialog();
                
                if (TopLevel.GetTopLevel(this) is Window parent)
                {
                    var result = await dialog.ShowDialog<bool>(parent);
                    
                    if (result && dialog.Result != null)
                    {
                        if (DataContext is TeacherManagementViewModel vm)
                        {
                            vm.AddTeacher(dialog.Result);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                // 处理错误 - 这里可以显示错误消息
                System.Diagnostics.Debug.WriteLine($"新增教师失败: {ex.Message}");
            }
        }

        private async void OnDeleteTeacherClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not TeacherManagementViewModel vm || vm.SelectedTeacher == null)
                return;

            try
            {
                if (TopLevel.GetTopLevel(this) is Window parent)
                {
                    var messageBox = new Window
                    {
                        Title = "确认删除",
                        Width = 350,
                        Height = 150,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        CanResize = false
                    };

                    var stackPanel = new StackPanel
                    {
                        Margin = new Avalonia.Thickness(20),
                        Spacing = 15
                    };

                    var messageText = new TextBlock
                    {
                        Text = $"确定要删除教师\"{vm.SelectedTeacher.Name}\"吗？\n此操作不可撤销。",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        TextAlignment = Avalonia.Media.TextAlignment.Center
                    };

                    var buttonPanel = new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Spacing = 10
                    };

                    var confirmButton = new Button
                    {
                        Content = "确定删除",
                        Background = Avalonia.Media.Brushes.Red,
                        Foreground = Avalonia.Media.Brushes.White,
                        Padding = new Avalonia.Thickness(15, 5)
                    };

                    var cancelButton = new Button
                    {
                        Content = "取消",
                        Padding = new Avalonia.Thickness(15, 5)
                    };

                    bool? result = null;

                    confirmButton.Click += (s, args) =>
                    {
                        result = true;
                        messageBox.Close();
                    };

                    cancelButton.Click += (s, args) =>
                    {
                        result = false;
                        messageBox.Close();
                    };

                    buttonPanel.Children.Add(confirmButton);
                    buttonPanel.Children.Add(cancelButton);

                    stackPanel.Children.Add(messageText);
                    stackPanel.Children.Add(buttonPanel);

                    messageBox.Content = stackPanel;

                    await messageBox.ShowDialog(parent);

                    if (result == true)
                    {
                        var teacherToDelete = vm.SelectedTeacher;
                        vm.DeleteTeacher(teacherToDelete);
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除教师失败: {ex.Message}");
            }
        }
    }
}
