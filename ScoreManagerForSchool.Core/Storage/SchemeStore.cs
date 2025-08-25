using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ScoreManagerForSchool.Core.Storage
{
    public class SchemeStore
    {
        private readonly EncryptedDataStore<List<string[]>> _store;
        private readonly string _baseDir;

        public SchemeStore(string baseDir)
        {
            _baseDir = baseDir;
            Directory.CreateDirectory(baseDir);
            _store = new EncryptedDataStore<List<string[]>>(baseDir, "schemes");
            
            // 检查是否需要数据迁移
            CheckAndMigrateData();
        }

        public List<string[]> Load()
        {
            return _store.Load() ?? new List<string[]>();
        }

        public void Save(IEnumerable<string[]> schemes)
        {
            _store.Save(new List<string[]>(schemes));
        }

        private void CheckAndMigrateData()
        {
            var jsonPath = Path.Combine(_baseDir, "schemes.json");
            if (File.Exists(jsonPath) && !_store.Exists())
            {
                DataMigrationHelper.MigrateData<List<string[]>>(_baseDir, "schemes.json", "schemes");
            }
        }
    }
}
