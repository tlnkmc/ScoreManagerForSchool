using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ScoreManagerForSchool.Core.Security;

namespace ScoreManagerForSchool.Core.Storage
{
    public class SecurityQuestionEntry
    {
        public string Question { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty; // base64
        public int Iterations { get; set; } = 100000;
        public string Hash { get; set; } = string.Empty; // base64 of derived key
    }

    public class SecurityQuestionsModel
    {
        public List<SecurityQuestionEntry> Items { get; set; } = new();
    }

    public class SecurityQuestionsStore
    {
        private readonly string _path;

        public SecurityQuestionsStore(string baseDir)
        {
            Directory.CreateDirectory(baseDir);
            _path = Path.Combine(baseDir, "secqa.json");
        }

        public SecurityQuestionsModel Load()
        {
            try
            {
                if (!File.Exists(_path)) return new SecurityQuestionsModel();
                var txt = File.ReadAllText(_path);
                var model = JsonSerializer.Deserialize<SecurityQuestionsModel>(txt);
                return model ?? new SecurityQuestionsModel();
            }
            catch { return new SecurityQuestionsModel(); }
        }

        public void Save(SecurityQuestionsModel model)
        {
            try
            {
                var txt = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_path, txt);
            }
            catch { }
        }

        public bool VerifyAnswers(IEnumerable<(string question, string answer)> qa)
        {
            try
            {
                var model = Load();
                var list = new List<(string q, string a)>(qa);
                if (model.Items.Count == 0 || list.Count == 0) return false;
                int ok = 0;
                foreach (var item in model.Items)
                {
                    // find matching question
                    var idx = list.FindIndex(x => string.Equals(x.q, item.Question, StringComparison.Ordinal));
                    if (idx < 0) continue;
                    var ans = list[idx].a ?? string.Empty;
                    var salt = string.IsNullOrEmpty(item.Salt) ? new byte[16] : Convert.FromBase64String(item.Salt);
                    var key = CryptoUtil.DeriveKey(ans, salt, 32, item.Iterations > 0 ? item.Iterations : 100000);
                    var b64 = Convert.ToBase64String(key);
                    if (string.Equals(b64, item.Hash, StringComparison.Ordinal)) ok++;
                }
                // require all configured questions answered correctly
                return ok == model.Items.Count;
            }
            catch { return false; }
        }
    }
}
