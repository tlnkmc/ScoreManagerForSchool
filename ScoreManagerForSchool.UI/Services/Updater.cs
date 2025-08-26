using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ScoreManagerForSchool.Core.Logging;

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
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
            Logger.LogDebug($"Current program version retrieved: {version}", "Updater");
            return version;
        }

        public static async Task<UpdateInfo?> CheckAsync(string feedUrl)
        {
            var currentVersion = GetCurrentVersion();
            Logger.LogVersionCheckStart(feedUrl, currentVersion);
            
            try
            {
                // feedUrl 可以是 ver.txt 的最终直链，也可以是仓库路径
                using var http = new HttpClient();
                Logger.LogDebug($"Sending HTTP request to: {feedUrl}", "Updater");
                
                var response = await http.GetAsync(feedUrl).ConfigureAwait(false);
                
                // 检查HTTP状态码
                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogWarning($"HTTP request failed - Status code: {response.StatusCode}", "Updater");
                    Logger.LogVersionCheckResult(currentVersion, null, false);
                    return null; // 非200状态码，返回null而不是抛出异常
                }
                
                Logger.LogDebug("HTTP request successful, parsing response content", "Updater");
                var txt = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Logger.LogDebug($"Response content length: {txt.Length} characters", "Updater");
                
                // 解析 INI 风格的键值
                var info = new UpdateInfo();
                using var sr = new StringReader(txt);
                string? line;
                var lineCount = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    lineCount++;
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                    var idx = line.IndexOf('=');
                    if (idx <= 0) continue;
                    var key = line.Substring(0, idx).Trim();
                    var val = line[(idx + 1)..].Trim();
                    
                    Logger.LogDebug($"Parsing key-value pair: {key} = {val}", "Updater");
                    
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
                
                Logger.LogDebug($"Parsing completed, processed {lineCount} lines, version: {info.Version}", "Updater");
                
                // 记录版本检查结果
                var hasUpdate = !string.IsNullOrEmpty(info.Version) && IsNewer(currentVersion, info.Version);
                Logger.LogVersionCheckResult(currentVersion, info.Version, hasUpdate);
                
                return info;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Exception occurred during version check: {ex.Message}", "Updater", ex);
                Logger.LogVersionCheckResult(currentVersion, null, false);
                return null;
            }
        }

        public static bool IsNewer(string current, string target)
        {
            Logger.LogDebug($"Comparing versions: current={current}, target={target}", "Updater");
            
            try
            {
                // 处理内部版本号格式 1.0.0.0010
                if (Version.TryParse(current, out var vCur) && Version.TryParse(target, out var vNew))
                {
                    // 比较主版本号（Major.Minor.Build.Revision）
                    var result = vNew > vCur;
                    Logger.LogDebug($"Version comparison result: {result} (parsed: {vCur} vs {vNew})", "Updater");
                    return result;
                }
                else
                {
                    Logger.LogWarning($"Version parsing failed, using string comparison - current: {current}, target: {target}", "Updater");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Exception during version comparison: {ex.Message}", "Updater", ex);
            }
            
            // fallback: simple compare
            var fallbackResult = !string.Equals(current, target, StringComparison.OrdinalIgnoreCase);
            Logger.LogDebug($"String comparison result: {fallbackResult}", "Updater");
            return fallbackResult;
        }

        public static string BuildSourceUrl(string raw, string source)
        {
            Logger.LogDebug($"Building source URL - Raw: {raw}, Source type: {source}", "Updater");
            
            // source: GitHub | Ghproxy | Custom
            if (string.IsNullOrWhiteSpace(raw)) 
            {
                Logger.LogWarning("Raw URL is empty", "Updater");
                return raw;
            }
            
            string result;
            if (string.Equals(source, "Ghproxy", StringComparison.OrdinalIgnoreCase))
            {
                if (raw.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    result = $"https://gh-proxy.com/{raw}";
                else
                    result = $"https://gh-proxy.com/https://{raw}";
            }
            else if (string.Equals(source, "GitHub", StringComparison.OrdinalIgnoreCase))
            {
                if (raw.StartsWith("http", StringComparison.OrdinalIgnoreCase)) 
                    result = raw;
                else
                    result = $"https://{raw}";
            }
            else
            {
                // Custom: 直接使用
                result = raw;
            }
            
            Logger.LogDebug($"Built URL: {result}", "Updater");
            return result;
        }

    public static async System.Threading.Tasks.Task<string?> DownloadAndPrepareUpdateAsync(UpdateInfo info, string source, string rootDir)
        {
            try
            {
                Logger.LogUpdatePreparation("Starting update preparation", $"Source: {source}, Directory: {rootDir}");
                
                // 清理 update 目录
        var updateDir = Path.Combine(rootDir, "update");
                if (Directory.Exists(updateDir)) 
                {
                    Logger.LogUpdatePreparation("Cleaning old update directory", updateDir);
                    TryDeleteDirectory(updateDir);
                }

                // 选择平台包
                string? url = null;
                string platform = "unknown";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    platform = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "win-arm64" : "win-x64";
                    url = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? info.Winarm : info.Winx64;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    platform = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
                    url = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? info.Macosarm : info.Macosx64;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    platform = "linux-x64";
                    url = info.Linuxx64;
                }

                if (string.IsNullOrWhiteSpace(url)) 
                {
                    Logger.LogError($"No update package URL found for platform {platform}", "Updater");
                    return null;
                }
                
                url = BuildSourceUrl(url!, source);
                Logger.LogUpdateDownloadStart(url, platform);

        Directory.CreateDirectory(rootDir);
                var ext = url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? ".zip" : ".tar.gz";
                var tmpPath = Path.Combine(rootDir, "download_tmp" + ext);
                
                Logger.LogDebug($"Temporary file path: {tmpPath}", "Updater");
                
                using (var http = new HttpClient())
                using (var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    // 检查HTTP状态码，非200时返回null而不是抛出异常
                    if (!resp.IsSuccessStatusCode)
                    {
                        Logger.LogUpdateDownloadResult(false, null, $"HTTP status code: {resp.StatusCode}");
                        return null;
                    }
                    
                    Logger.LogDebug("Starting file content download", "Updater");
                    await using var fs = File.Create(tmpPath);
                    await resp.Content.CopyToAsync(fs).ConfigureAwait(false);
                    Logger.LogDebug($"File download completed, size: {fs.Length} bytes", "Updater");
                }
                
                // 重命名为 update（不含扩展名）
                var updatePkg = Path.Combine(rootDir, "update");
                if (File.Exists(updatePkg)) 
                {
                    try 
                    { 
                        File.Delete(updatePkg);
                        Logger.LogDebug("Deleted existing update package file", "Updater");
                    } 
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Cannot delete existing update package file: {ex.Message}", "Updater");
                    }
                }
                
                File.Move(tmpPath, updatePkg, true);
                Logger.LogUpdateDownloadResult(true, updatePkg);
                return updatePkg;
            }
            catch (Exception ex)
            {
                Logger.LogUpdateDownloadResult(false, null, ex.Message);
                return null;
            }
        }

    public static void StartUpdaterU(string programDir)
        {
            try
            {
                Logger.LogUpdatePreparation("Starting updater program", $"Program directory: {programDir}");
                
                var exe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "update.exe" : "update";
        var path = Path.Combine(programDir, exe);
                if (!File.Exists(path))
                {
                    // 兼容：如果更新器随主程序分发在同目录
                    path = Path.Combine(AppContext.BaseDirectory, exe);
                    Logger.LogDebug($"Updater not in program directory, trying: {path}", "Updater");
                }
                
                if (!File.Exists(path))
                {
                    Logger.LogError($"Updater program not found: {path}", "Updater");
                    return;
                }
                
                var token = DateTime.UtcNow.Ticks.ToString();
                var arguments = $"-u --token {token}";
                Logger.LogInfo($"Starting updater program: {path} {arguments}", "Updater");
                
                var psi = new ProcessStartInfo(path, arguments)
                {
                    WorkingDirectory = AppContext.BaseDirectory,
                    UseShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                };
                Process.Start(psi);
                Logger.LogInfo("Updater program started", "Updater");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to start updater program: {ex.Message}", "Updater", ex);
            }
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
