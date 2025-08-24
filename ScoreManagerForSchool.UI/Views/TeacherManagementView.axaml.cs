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
    }
}
