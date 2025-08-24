using System;
using System.IO;
using System.Text.Json;

namespace ScoreManagerForSchool.Core.Storage
{
    public class Database1Store
    {
        private readonly string _path;

        public Database1Store(string baseDir)
        {
            Directory.CreateDirectory(baseDir);
            _path = Path.Combine(baseDir, "Database1.json");
        }

        public Database1Model? Load()
        {
            if (!File.Exists(_path)) return null;
            var txt = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<Database1Model>(txt);
        }

        public void Save(Database1Model model)
        {
            var txt = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_path, txt);
        }

        // Utility: remove common base files created by the app
        public static void DeleteBaseFiles(string? baseDir = null)
        {
            try
            {
                var dir = string.IsNullOrEmpty(baseDir) ? Path.Combine(Directory.GetCurrentDirectory(), "base") : baseDir;
                if (!Directory.Exists(dir)) return;
                var files = new[] { "Database1.json", "pwhint1.ini", "students.json", "classes.json", "schemes.json", "secqa.json" };
                foreach (var f in files)
                {
                    var p = Path.Combine(dir, f);
                    if (File.Exists(p)) File.Delete(p);
                }
            }
            catch { }
        }
    }
}
