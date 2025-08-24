using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace ScoreManagerForSchool.UI.Services
{
    public static class FilePickerUtil
    {
        public static async Task<string?> PickCsvOrExcelToLocalPathAsync(TopLevel topLevel, string title)
        {
            if (topLevel?.StorageProvider == null) return null;
            var options = new FilePickerOpenOptions
            {
                Title = string.IsNullOrWhiteSpace(title) ? "选择 CSV/Excel 文件" : title,
                AllowMultiple = false,
                FileTypeFilter = new System.Collections.Generic.List<FilePickerFileType>
                {
                    new FilePickerFileType("CSV/Excel")
                    {
                        Patterns = new System.Collections.Generic.List<string>{"*.csv","*.xls","*.xlsx"},
                        MimeTypes = new System.Collections.Generic.List<string>{
                            "text/csv","application/vnd.ms-excel","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                        }
                    }
                }
            };
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
            if (files == null || files.Count == 0) return null;

            var file = files[0];
            // 尝试获取本地路径；若不可用，复制到临时文件
            try
            {
                var local = file.TryGetLocalPath();
                if (!string.IsNullOrWhiteSpace(local))
                {
                    try { if (Uri.TryCreate(local, UriKind.Absolute, out var u) && u.IsFile) local = u.LocalPath; } catch { }
                    try { local = Path.GetFullPath(local); } catch { }
                    return local;
                }
            }
            catch { }

            // 复制到临时目录并返回
            try
            {
                var ext = ".tmp";
                try { var n = file.Name; if (!string.IsNullOrEmpty(n)) ext = Path.GetExtension(n) ?? ext; } catch { }
                var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ext);
                await using var stream = await file.OpenReadAsync();
                using var fs = File.Create(tmp);
                await stream.CopyToAsync(fs);
                return Path.GetFullPath(tmp);
            }
            catch { }

            return null;
        }
    }
}
