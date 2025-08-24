using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ScoreManagerForSchool.Core.Storage
{
    public class EvaluationEntry
    {
        public string? Id { get; set; }            // 用于编辑功能的唯一标识
        public string? Class { get; set; }
        public string? StudentId { get; set; }
        public string? Name { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string? Item { get; set; }
        public double Score { get; set; }
        public string? Remark { get; set; }
        
        // 新增教师和科目组字段
        public string? TeacherId { get; set; }     // 任课教师ID
        public string? TeacherName { get; set; }   // 任课教师姓名
        public string? Subject { get; set; }       // 科目
        public string? SubjectGroup { get; set; }  // 科目组（语文、数学等）
    }

    public class EvaluationStore
    {
        private readonly string _path;

        public EvaluationStore(string baseDir)
        {
            Directory.CreateDirectory(baseDir);
            _path = Path.Combine(baseDir, "evaluations.json");
        }

        public List<EvaluationEntry> Load()
        {
            if (!File.Exists(_path)) return new List<EvaluationEntry>();
            return JsonSerializer.Deserialize<List<EvaluationEntry>>(File.ReadAllText(_path)) ?? new List<EvaluationEntry>();
        }

        public void Save(IEnumerable<EvaluationEntry> entries)
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
