using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;

namespace ScoreManagerForSchool.Core.Logging
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string? _logFilePath;
        private static bool _initialized = false;
        private static readonly Encoding _utf8WithBom = new UTF8Encoding(true); // 使用带BOM的UTF-8解决乱码问题

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                var programDir = AppDomain.CurrentDomain.BaseDirectory;
                var logsDir = Path.Combine(programDir, "logs");
                Directory.CreateDirectory(logsDir);

                var startTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                _logFilePath = Path.Combine(logsDir, $"smfs_{startTime}.log"); // 改回.log扩展名，确保正确的UTF-8编码

                // 先写入UTF-8 BOM标记，确保文件被识别为UTF-8
                File.WriteAllText(_logFilePath, "", _utf8WithBom);
                
                // 写入启动日志（使用英文避免编码问题）
                WriteToFile($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [Logger] Application started");
                WriteToFile($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [Logger] Program directory: {programDir}");
                WriteToFile($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [Logger] Log file: {_logFilePath}");
                WriteToFile($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [Logger] Log encoding: UTF-8 with BOM");

                _initialized = true;
            }
            catch (Exception ex)
            {
                // 如果日志初始化失败，尝试写入临时目录
                try
                {
                    var tempDir = Path.GetTempPath();
                    _logFilePath = Path.Combine(tempDir, $"smfs_fallback_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
                    // 先写入UTF-8 BOM
                    File.WriteAllText(_logFilePath, "", _utf8WithBom);
                    WriteToFile($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [ERROR] [Logger] Log initialization failed, using temp directory: {ex.Message}");
                }
                catch
                {
                    // 完全无法写入日志，设置为null
                    _logFilePath = null;
                }
            }
        }

        public static void LogInfo(string message, string? source = null)
        {
            LogMessage("INFO", message, source);
        }

        public static void LogWarning(string message, string? source = null)
        {
            LogMessage("WARN", message, source);
        }

        public static void LogError(string message, string? source = null, Exception? exception = null)
        {
            var fullMessage = message;
            if (exception != null)
            {
                fullMessage += $"\n异常详情: {exception}";
            }
            LogMessage("ERROR", fullMessage, source);
        }

        public static void LogDebug(string message, string? source = null)
        {
#if DEBUG
            LogMessage("DEBUG", message, source);
#endif
        }

        private static void LogMessage(string level, string message, string? source)
        {
            if (!_initialized)
            {
                Initialize();
            }

            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                
                // 使用简化的文本格式，避免控制台输出问题
                var logEntry = $"[{timestamp}] [{level}]";
                if (!string.IsNullOrWhiteSpace(source))
                {
                    logEntry += $" [{source}]";
                }
                logEntry += $" {message}";

                WriteToFile(logEntry);

                // 在Debug模式下输出到控制台
#if DEBUG
                Console.WriteLine(logEntry);
#endif
            }
            catch
            {
                // 忽略日志写入失败
            }
        }

        private static void WriteToFile(string content)
        {
            if (string.IsNullOrEmpty(_logFilePath)) return;

            lock (_lock)
            {
                try
                {
                    // 使用FileStream确保正确的UTF-8编码写入
                    using (var stream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(stream, _utf8WithBom))
                    {
                        writer.WriteLine(content);
                        writer.Flush();
                    }
                }
                catch
                {
                    // 忽略写入失败
                }
            }
        }

        public static string? GetCurrentLogPath()
        {
            return _logFilePath;
        }

        public static void LogApplicationShutdown()
        {
            LogInfo("Application shutdown");
        }

        // 新增：详细版本检查日志方法
        public static void LogVersionCheckStart(string feedUrl, string currentVersion)
        {
            LogInfo($"Version check started - Current version: {currentVersion}, Feed URL: {feedUrl}", "Updater");
        }

        public static void LogVersionCheckResult(string currentVersion, string? remoteVersion, bool hasUpdate)
        {
            if (remoteVersion == null)
            {
                LogWarning("Version check failed - Unable to get remote version info", "Updater");
            }
            else
            {
                var result = hasUpdate ? "New version available" : "Already latest version";
                LogInfo($"Version check completed - Current: {currentVersion}, Remote: {remoteVersion}, Result: {result}", "Updater");
            }
        }

        public static void LogUpdateDownloadStart(string url, string platform)
        {
            LogInfo($"Update download started - Platform: {platform}, URL: {url}", "Updater");
        }

        public static void LogUpdateDownloadResult(bool success, string? filePath = null, string? error = null)
        {
            if (success && filePath != null)
            {
                LogInfo($"Update download completed - File: {filePath}", "Updater");
            }
            else
            {
                LogError($"Update download failed - Error: {error ?? "Unknown error"}", "Updater");
            }
        }

        public static void LogUpdatePreparation(string action, string? details = null)
        {
            var message = string.IsNullOrEmpty(details) ? action : $"{action} - {details}";
            LogInfo(message, "Updater");
        }
    }
}
