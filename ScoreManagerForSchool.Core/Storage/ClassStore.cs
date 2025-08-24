using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ScoreManagerForSchool.Core.Storage
{
    public class ClassStore
    {
        private readonly string _path;

        public ClassStore(string baseDir)
        {
            Directory.CreateDirectory(baseDir);
            _path = Path.Combine(baseDir, "classes.json");
        }

        public List<ClassInfo> Load()
        {
            if (!File.Exists(_path)) return new List<ClassInfo>();
            return JsonSerializer.Deserialize<List<ClassInfo>>(File.ReadAllText(_path)) ?? new List<ClassInfo>();
        }

        public void Save(IEnumerable<ClassInfo> classes)
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(classes, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
