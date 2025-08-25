using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ScoreManagerForSchool.Core.Storage
{
    public class SubjectGroupStore
    {
        private readonly EncryptedDataStore<List<SubjectGroup>> _store;
        private readonly string _baseDir;

        public SubjectGroupStore(string baseDir)
        {
            _baseDir = baseDir;
            Directory.CreateDirectory(baseDir);
            _store = new EncryptedDataStore<List<SubjectGroup>>(baseDir, "subject_groups");
            
            // 检查是否需要数据迁移
            CheckAndMigrateData();
        }

        public List<SubjectGroup> Load()
        {
            var groups = _store.Load();
            if (groups == null || groups.Count == 0) 
            {
                // 创建默认科目组
                var defaultGroups = GetDefaultSubjectGroups();
                Save(defaultGroups);
                return defaultGroups;
            }
            
            return groups;
        }

        public void Save(IEnumerable<SubjectGroup> subjectGroups)
        {
            _store.Save(new List<SubjectGroup>(subjectGroups));
        }

        public void ResetToDefault()
        {
            var defaults = GetDefaultSubjectGroups();
            Save(defaults);
        }

        private void CheckAndMigrateData()
        {
            var jsonPath = Path.Combine(_baseDir, "subject_groups.json");
            if (File.Exists(jsonPath) && !_store.Exists())
            {
                DataMigrationHelper.MigrateData<List<SubjectGroup>>(_baseDir, "subject_groups.json", "subject_groups");
            }
        }

        // 获取默认科目组配置
        private List<SubjectGroup> GetDefaultSubjectGroups()
        {
            // 仅包含指定的九项科目组；“外语”包含“英语”作为匹配科目
            return new List<SubjectGroup>
            {
                new SubjectGroup { Name = "语文", Description = "语文科目组", Subjects = new List<string> { "语文" } },
                new SubjectGroup { Name = "数学", Description = "数学科目组", Subjects = new List<string> { "数学" } },
                new SubjectGroup { Name = "外语", Description = "外语科目组", Subjects = new List<string> { "外语", "英语" } },
                new SubjectGroup { Name = "物理", Description = "物理科目组", Subjects = new List<string> { "物理" } },
                new SubjectGroup { Name = "历史", Description = "历史科目组", Subjects = new List<string> { "历史" } },
                new SubjectGroup { Name = "化学", Description = "化学科目组", Subjects = new List<string> { "化学" } },
                new SubjectGroup { Name = "政治", Description = "政治科目组", Subjects = new List<string> { "政治" } },
                new SubjectGroup { Name = "生物", Description = "生物科目组", Subjects = new List<string> { "生物" } },
                new SubjectGroup { Name = "地理", Description = "地理科目组", Subjects = new List<string> { "地理" } },
            };
        }

        // 根据科目名查找对应的科目组
        public string? GetSubjectGroupBySubject(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject)) return null;
            
            var groups = Load();
            var group = groups.FirstOrDefault(g => 
                g.Subjects.Any(s => s.Equals(subject, StringComparison.OrdinalIgnoreCase)));
            
            return group?.Name;
        }

        // 根据科目组名获取该组下的所有科目
        public List<string> GetSubjectsByGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return new List<string>();
            
            var groups = Load();
            var group = groups.FirstOrDefault(g => 
                g.Name?.Equals(groupName, StringComparison.OrdinalIgnoreCase) == true);
            
            return group?.Subjects ?? new List<string>();
        }
    }
}
