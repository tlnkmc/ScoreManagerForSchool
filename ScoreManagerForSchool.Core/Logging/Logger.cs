using System;
using System.IO;
using System.Threading.Tasks;

namespace ScoreManagerForSchool.Core.Logging
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string? _logFilePath;
        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                var programDir = AppDomain.CurrentDomain.BaseDirectory;
                var logsDir = Path.Combine(programDir, "logs");
                Directory.CreateDirectory(logsDir);

                var startTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                _logFilePath = Path.Combine(logsDir, $"smfs_{startTime}.log");

                // 写入启动日志
                WriteToFile($"=== 应用程序启动 === {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                WriteToFile($"程序目录: {programDir}");
                WriteToFile($"日志文件: {_logFilePath}");

                _initialized = true;
            }
            catch (Exception ex)
            {
                // 如果日志初始化失败，尝试写入临时目录
                try
                {
                    var tempDir = Path.GetTempPath();
                    _logFilePath = Path.Combine(tempDir, $"smfs_fallback_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
                    WriteToFile($"日志初始化失败，使用临时目录: {ex.Message}");
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
                var sourceInfo = string.IsNullOrWhiteSpace(source) ? "" : $" [{source}]";
                var logEntry = $"[{timestamp}] [{level}]{sourceInfo} {message}";

                WriteToFile(logEntry);

                // 在Debug模式下也输出到控制台
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
                    File.AppendAllText(_logFilePath, content + Environment.NewLine, System.Text.Encoding.UTF8);
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
            LogInfo("=== 应用程序关闭 ===");
        }
    }
}
