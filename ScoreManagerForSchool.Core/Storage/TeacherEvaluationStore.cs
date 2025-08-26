using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScoreManagerForSchool.Core.Storage
{
    public class TeacherEvaluationStore
    {
        private readonly EncryptedDataStore<List<TeacherEvaluationEntry>> _store;
        private readonly string _baseDir;

        public TeacherEvaluationStore(string baseDir)
        {
            _baseDir = baseDir;
            Directory.CreateDirectory(baseDir);
            _store = new EncryptedDataStore<List<TeacherEvaluationEntry>>(baseDir, "teacher_evaluations");
        }

        public List<TeacherEvaluationEntry> Load()
        {
            return _store.Load() ?? new List<TeacherEvaluationEntry>();
        }

        public void Save(IEnumerable<TeacherEvaluationEntry> entries)
        {
            var list = entries.ToList();
            _store.Save(list);
        }

        public void Add(TeacherEvaluationEntry entry)
        {
            var entries = Load();
            
            // 生成唯一ID
            if (string.IsNullOrEmpty(entry.Id))
            {
                entry.Id = Guid.NewGuid().ToString("N")[..8];
            }
            
            entries.Add(entry);
            Save(entries);
        }

        public void Update(TeacherEvaluationEntry entry)
        {
            var entries = Load();
            var existing = entries.FirstOrDefault(e => e.Id == entry.Id);
            if (existing != null)
            {
                var index = entries.IndexOf(existing);
                entries[index] = entry;
                Save(entries);
            }
        }

        public void Delete(string id)
        {
            var entries = Load();
            var toRemove = entries.FirstOrDefault(e => e.Id == id);
            if (toRemove != null)
            {
                entries.Remove(toRemove);
                Save(entries);
            }
        }

        // 按教师获取积分记录
        public List<TeacherEvaluationEntry> GetByTeacher(string teacherName)
        {
            return Load().Where(e => 
                string.Equals(e.TeacherName, teacherName, StringComparison.OrdinalIgnoreCase)
            ).OrderByDescending(e => e.Date).ToList();
        }

        // 按班级获取教师积分记录
        public List<TeacherEvaluationEntry> GetByClass(string className)
        {
            return Load().Where(e => 
                string.Equals(e.Class, className, StringComparison.OrdinalIgnoreCase)
            ).OrderByDescending(e => e.Date).ToList();
        }

        // 获取教师积分统计
        public Dictionary<string, double> GetTeacherScoreSummary(DateTime? startDate = null, DateTime? endDate = null)
        {
            var entries = Load();
            var query = entries.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.Date >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(e => e.Date <= endDate.Value);

            return query
                .Where(e => !string.IsNullOrEmpty(e.TeacherName))
                .GroupBy(e => e.TeacherName!)
                .ToDictionary(g => g.Key, g => g.Sum(e => e.Score));
        }

        // 获取科目组积分统计
        public Dictionary<string, double> GetSubjectGroupScoreSummary(DateTime? startDate = null, DateTime? endDate = null)
        {
            var entries = Load();
            var query = entries.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.Date >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(e => e.Date <= endDate.Value);

            return query
                .Where(e => !string.IsNullOrEmpty(e.SubjectGroup))
                .GroupBy(e => e.SubjectGroup!)
                .ToDictionary(g => g.Key, g => g.Sum(e => e.Score));
        }
    }
}
