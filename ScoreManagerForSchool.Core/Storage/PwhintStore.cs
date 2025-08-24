using System;
using System.Collections.Generic;
using System.IO;

namespace ScoreManagerForSchool.Core.Storage
{
    public class PwhintStore
    {
        private readonly string _path;

        public PwhintStore(string baseDir)
        {
            Directory.CreateDirectory(baseDir);
            _path = Path.Combine(baseDir, "pwhint1.ini");
        }

        public void SaveHints(IEnumerable<string?> hints)
        {
            using var sw = new StreamWriter(_path, false);
            foreach (var h in hints)
            {
                if (string.IsNullOrEmpty(h)) { sw.WriteLine(); continue; }
                var b = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(h));
                sw.WriteLine(b);
            }
        }

        public List<string?> LoadHints()
        {
            var list = new List<string?>();
            if (!File.Exists(_path)) return list;
            foreach (var line in File.ReadAllLines(_path))
            {
                if (string.IsNullOrWhiteSpace(line)) { list.Add(null); continue; }
                try
                {
                    var txt = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(line.Trim()));
                    list.Add(txt);
                }
                catch { list.Add(null); }
            }
            return list;
        }
    }
}
