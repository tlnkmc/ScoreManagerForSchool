using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScoreManagerForSchool.Core.Storage
{
    public class EvaluationEntry
    {
        public string? Id { get; set; }            // 用于编辑功能的唯一标识
        public string? Class { get; set; }
        public string? StudentId { get; set; }
        public string? Name { get; set; }
        
        [JsonConverter(typeof(DateTimeJsonConverter))]
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
        private readonly EncryptedDataStore<List<EvaluationEntry>> _store;
        private readonly string _baseDir;

        public EvaluationStore(string baseDir)
        {
            _baseDir = baseDir;
            Directory.CreateDirectory(baseDir);
            _store = new EncryptedDataStore<List<EvaluationEntry>>(baseDir, "evaluations");
            
            // 检查是否需要数据迁移
            CheckAndMigrateData();
        }

        public List<EvaluationEntry> Load()
        {
            return _store.Load() ?? new List<EvaluationEntry>();
        }

        public void Save(IEnumerable<EvaluationEntry> entries)
        {
            _store.Save(new List<EvaluationEntry>(entries));
        }

        private void CheckAndMigrateData()
        {
            var jsonPath = Path.Combine(_baseDir, "evaluations.json");
            if (File.Exists(jsonPath) && !_store.Exists())
            {
                DataMigrationHelper.MigrateData<List<EvaluationEntry>>(_baseDir, "evaluations.json", "evaluations");
            }
        }
    }
}
