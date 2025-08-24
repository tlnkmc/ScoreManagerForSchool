using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ScoreManagerForSchool.Core.Storage
{
    public class StudentStore
    {
        private readonly string _path;

        public StudentStore(string baseDir)
        {
            Directory.CreateDirectory(baseDir);
            _path = Path.Combine(baseDir, "students.json");
        }

        public List<Student> Load()
        {
            if (!File.Exists(_path)) return new List<Student>();
            var list = JsonSerializer.Deserialize<List<Student>>(File.ReadAllText(_path)) ?? new List<Student>();
            bool changed = false;
            foreach (var s in list)
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
                try { Save(list); } catch { }
            }
            return list;
        }

        public void Save(IEnumerable<Student> students)
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(students, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
