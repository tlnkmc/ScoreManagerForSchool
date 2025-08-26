using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ScoreManagerForSchool.Core.Tools;

namespace ScoreManagerForSchool.Core.Storage
{
    public class StudentStore
    {
        private readonly EncryptedDataStore<List<Student>> _store;
        private readonly string _baseDir;

        public StudentStore(string baseDir)
        {
            _baseDir = baseDir;
            Directory.CreateDirectory(baseDir);
            _store = new EncryptedDataStore<List<Student>>(baseDir, "students");
            
            // 检查是否需要数据迁移
            CheckAndMigrateData();
        }

        public List<Student> Load()
        {
            var students = _store.Load() ?? new List<Student>();
            bool changed = false;
            
            foreach (var s in students)
            {
                if (s == null) continue;
                var wantFull = PinyinUtil.Full(s.Name);
                var wantInit = PinyinUtil.Initials(s.Name);
                if (!string.Equals(s.NamePinyin, wantFull, StringComparison.Ordinal))
                { s.NamePinyin = wantFull; changed = true; }
                if (!string.Equals(s.NamePinyinInitials, wantInit, StringComparison.Ordinal))
                { s.NamePinyinInitials = wantInit; changed = true; }
            }
            
            if (changed)
            {
                try { Save(students); } catch { }
            }
            
            return students;
        }

        public void Save(IEnumerable<Student> students)
        {
            _store.Save(new List<Student>(students));
        }

        private void CheckAndMigrateData()
        {
            var jsonPath = Path.Combine(_baseDir, "students.json");
            if (File.Exists(jsonPath) && !_store.Exists())
            {
                DataMigrationHelper.MigrateData<List<Student>>(_baseDir, "students.json", "students");
            }
        }
    }
}
