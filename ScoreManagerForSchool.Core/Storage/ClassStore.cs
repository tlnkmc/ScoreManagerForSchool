using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ScoreManagerForSchool.Core.Storage
{
    public class ClassStore
    {
        private readonly EncryptedDataStore<List<ClassInfo>> _store;
        private readonly string _baseDir;

        public ClassStore(string baseDir)
        {
            _baseDir = baseDir;
            Directory.CreateDirectory(baseDir);
            _store = new EncryptedDataStore<List<ClassInfo>>(baseDir, "classes");
            
            // 检查是否需要数据迁移
            CheckAndMigrateData();
        }

        public List<ClassInfo> Load()
        {
            return _store.Load() ?? new List<ClassInfo>();
        }

        public void Save(IEnumerable<ClassInfo> classes)
        {
            _store.Save(new List<ClassInfo>(classes));
        }

        private void CheckAndMigrateData()
        {
            var jsonPath = Path.Combine(_baseDir, "classes.json");
            if (File.Exists(jsonPath) && !_store.Exists())
            {
                DataMigrationHelper.MigrateData<List<ClassInfo>>(_baseDir, "classes.json", "classes");
            }
        }
    }
}
