using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ScoreManagerForSchool.Core.Storage
{
    public class SchemeStore
    {
        private readonly string _path;

        public SchemeStore(string baseDir)
        {
            Directory.CreateDirectory(baseDir);
            _path = Path.Combine(baseDir, "schemes.json");
        }

        public List<string[]> Load()
        {
            if (!File.Exists(_path)) return new List<string[]>();
            return JsonSerializer.Deserialize<List<string[]>>(File.ReadAllText(_path)) ?? new List<string[]>();
        }

        public void Save(IEnumerable<string[]> schemes)
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(schemes, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
