using System;
using System.IO;
using System.Text.Json;

namespace ScoreManagerForSchool.Core.Storage
{
    public class AppConfig
    {
        public string Theme { get; set; } = "System"; // System | Light | Dark
        public bool StudentsCsvHeaderDefault { get; set; } = true;
        public bool EnableAcrylicEffect { get; set; } = false; // 启用亚克力效果
    // Advanced settings
    public bool AutoStart { get; set; } = false;
    public bool AutoUpdateCheck { get; set; } = false;
    public string? UpdateFeedUrl { get; set; } = null; // ver.txt 的下载地址（自定义源时使用）
    public string UpdateSource { get; set; } = "GitHub"; // GitHub | Ghproxy | Custom
    public string? ThemeAccent { get; set; } = null; // Hex color like #0078D4 or named preset
    }

    public class AppConfigStore
    {
        private readonly string _path;

        public AppConfigStore(string baseDir)
        {
            Directory.CreateDirectory(baseDir);
            _path = Path.Combine(baseDir, "appconfig.json");
        }

        public AppConfig Load()
        {
            try
            {
                if (!File.Exists(_path)) return new AppConfig();
                var txt = File.ReadAllText(_path);
                return JsonSerializer.Deserialize<AppConfig>(txt) ?? new AppConfig();
            }
            catch { return new AppConfig(); }
        }

        public void Save(AppConfig cfg)
        {
            try
            {
                var txt = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_path, txt);
            }
            catch { }
        }
    }
}
