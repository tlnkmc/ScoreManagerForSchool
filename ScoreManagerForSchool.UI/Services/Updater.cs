using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ScoreManagerForSchool.UI.Services
{
    public class UpdateInfo
    {
        public string? Version { get; set; }
        public string? Winx64 { get; set; }
        public string? Winarm { get; set; }
        public string? Linuxx64 { get; set; }
        public string? Macosx64 { get; set; }
        public string? Macosarm { get; set; }
        public string? Notes { get; set; }
    }

    public static class Updater
    {
        public static string GetCurrentVersion()
            => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";

        public static async Task<UpdateInfo?> CheckAsync(string feedUrl)
        {
            try
            {
                // feedUrl 可以是 ver.txt 的最终直链，也可以是仓库路径
                using var http = new HttpClient();
                var response = await http.GetAsync(feedUrl).ConfigureAwait(false);
                
                // 检查HTTP状态码
                if (!response.IsSuccessStatusCode)
                {
                    return null; // 非200状态码，返回null而不是抛出异常
                }
                
                var txt = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                // 解析 INI 风格的键值
                var info = new UpdateInfo();
                using var sr = new StringReader(txt);
                string? line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                    var idx = line.IndexOf('=');
                    if (idx <= 0) continue;
                    var key = line.Substring(0, idx).Trim();
                    var val = line[(idx + 1)..].Trim();
                    switch (key.ToLowerInvariant())
                    {
                        case "version": info.Version = val; break;
                        case "winx64": info.Winx64 = val; break;
                        case "winarm": info.Winarm = val; break;
                        case "linuxx64": info.Linuxx64 = val; break;
                        case "macosx64": info.Macosx64 = val; break;
                        case "macosarm": info.Macosarm = val; break;
                        case "notes": info.Notes = val; break;
                    }
                }
                return info;
            }
            catch
            {
                return null;
            }
        }

        public static bool IsNewer(string current, string target)
        {
            try
            {
                // 处理内部版本号格式 1.0.0.0010
                if (Version.TryParse(current, out var vCur) && Version.TryParse(target, out var vNew))
                {
                    // 比较主版本号（Major.Minor.Build.Revision）
                    return vNew > vCur;
                }
            }
            catch
            {
                // 解析失败时使用字符串比较作为兜底
            }
            
            // fallback: simple compare
            return !string.Equals(current, target, StringComparison.OrdinalIgnoreCase);
        }

        public static string BuildSourceUrl(string raw, string source)
        {
            // source: GitHub | Ghproxy | Custom
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            if (string.Equals(source, "Ghproxy", StringComparison.OrdinalIgnoreCase))
            {
                if (raw.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    return $"https://gh-proxy.com/{raw}";
                return $"https://gh-proxy.com/https://{raw}";
            }
            if (string.Equals(source, "GitHub", StringComparison.OrdinalIgnoreCase))
            {
                if (raw.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return raw;
                return $"https://{raw}";
            }
            // Custom: 直接使用
            return raw;
        }

    public static async System.Threading.Tasks.Task<string?> DownloadAndPrepareUpdateAsync(UpdateInfo info, string source, string rootDir)
        {
            try
            {
                // 清理 update 目录
        var updateDir = Path.Combine(rootDir, "update");
                if (Directory.Exists(updateDir)) TryDeleteDirectory(updateDir);

                // 选择平台包
                string? url = null;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    url = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? info.Winarm : info.Winx64;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    url = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? info.Macosarm : info.Macosx64;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    url = info.Linuxx64;

                if (string.IsNullOrWhiteSpace(url)) return null;
                url = BuildSourceUrl(url!, source);

        Directory.CreateDirectory(rootDir);
                var ext = url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? ".zip" : ".tar.gz";
                var tmpPath = Path.Combine(rootDir, "download_tmp" + ext);
                using (var http = new HttpClient())
                using (var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    // 检查HTTP状态码，非200时返回null而不是抛出异常
                    if (!resp.IsSuccessStatusCode)
                    {
                        return null;
                    }
                    
                    await using var fs = File.Create(tmpPath);
                    await resp.Content.CopyToAsync(fs).ConfigureAwait(false);
                }
                // 重命名为 update（不含扩展名）
                var updatePkg = Path.Combine(rootDir, "update");
                if (File.Exists(updatePkg)) try { File.Delete(updatePkg); } catch { }
                File.Move(tmpPath, updatePkg, true);
                return updatePkg;
            }
            catch
            {
                return null;
            }
        }

    public static void StartUpdaterU(string programDir)
        {
            try
            {
                var exe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "update.exe" : "update";
        var path = Path.Combine(programDir, exe);
                if (!File.Exists(path))
                {
                    // 兼容：如果更新器随主程序分发在同目录
                    path = Path.Combine(AppContext.BaseDirectory, exe);
                }
                var token = DateTime.UtcNow.Ticks.ToString();
                var psi = new ProcessStartInfo(path, $"-u --token {token}")
                {
                    WorkingDirectory = AppContext.BaseDirectory,
                    UseShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                };
                Process.Start(psi);
            }
            catch { }
        }

        private static void TryDeleteDirectory(string dir)
        {
            try
            {
                if (!Directory.Exists(dir)) return;
                foreach (var sub in Directory.GetDirectories(dir)) TryDeleteDirectory(sub);
                foreach (var file in Directory.GetFiles(dir)) { try { File.Delete(file); } catch { } }
                Directory.Delete(dir, true);
            }
            catch { }
        }
    }
}
