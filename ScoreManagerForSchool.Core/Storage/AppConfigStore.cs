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
        private readonly EncryptedDataStore<AppConfig> _store;
        private readonly string _baseDir;

        public AppConfigStore(string baseDir)
        {
            _baseDir = baseDir;
            Directory.CreateDirectory(baseDir);
            _store = new EncryptedDataStore<AppConfig>(baseDir, "appconfig");
            
            // 检查是否需要数据迁移
            CheckAndMigrateData();
        }

        public AppConfig Load()
        {
            try
            {
                return _store.Load() ?? new AppConfig();
            }
            catch { return new AppConfig(); }
        }

        public void Save(AppConfig cfg)
        {
            try
            {
                _store.Save(cfg);
            }
            catch { }
        }

        private void CheckAndMigrateData()
        {
            var jsonPath = Path.Combine(_baseDir, "appconfig.json");
            if (File.Exists(jsonPath) && !_store.Exists())
            {
                DataMigrationHelper.MigrateData<AppConfig>(_baseDir, "appconfig.json", "appconfig");
            }
        }
    }
}
