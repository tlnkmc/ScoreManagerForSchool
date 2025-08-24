using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ScoreManagerForSchool.Core.Storage
{
    public class SubjectGroupStore
    {
        private readonly string _path;

        public SubjectGroupStore(string baseDir)
        {
            Directory.CreateDirectory(baseDir);
            _path = Path.Combine(baseDir, "subject_groups.json");
        }

        public List<SubjectGroup> Load()
        {
            if (!File.Exists(_path)) 
            {
                // 创建默认科目组
                var defaultGroups = GetDefaultSubjectGroups();
                Save(defaultGroups);
                return defaultGroups;
            }
            
            try
            {
                return JsonSerializer.Deserialize<List<SubjectGroup>>(File.ReadAllText(_path)) ?? new List<SubjectGroup>();
            }
            catch
            {
                return GetDefaultSubjectGroups();
            }
        }

        public void Save(IEnumerable<SubjectGroup> subjectGroups)
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(subjectGroups, new JsonSerializerOptions { WriteIndented = true }));
        }

        // 获取默认科目组配置
        private List<SubjectGroup> GetDefaultSubjectGroups()
        {
            return new List<SubjectGroup>
            {
                new SubjectGroup 
                { 
                    Name = "语文", 
                    Description = "语文科目组", 
                    Subjects = new List<string> { "语文", "阅读", "写作", "古诗词" } 
                },
                new SubjectGroup 
                { 
                    Name = "数学", 
                    Description = "数学科目组", 
                    Subjects = new List<string> { "数学", "几何", "代数", "应用数学" } 
                },
                new SubjectGroup 
                { 
                    Name = "英语", 
                    Description = "英语科目组", 
                    Subjects = new List<string> { "英语", "英语口语", "英语听力", "英语写作" } 
                },
                new SubjectGroup 
                { 
                    Name = "理科", 
                    Description = "理科科目组", 
                    Subjects = new List<string> { "物理", "化学", "生物", "科学" } 
                },
                new SubjectGroup 
                { 
                    Name = "文科", 
                    Description = "文科科目组", 
                    Subjects = new List<string> { "历史", "地理", "政治", "社会" } 
                },
                new SubjectGroup 
                { 
                    Name = "艺体", 
                    Description = "艺术体育科目组", 
                    Subjects = new List<string> { "美术", "音乐", "体育", "舞蹈", "书法" } 
                },
                new SubjectGroup 
                { 
                    Name = "技术", 
                    Description = "技术科目组", 
                    Subjects = new List<string> { "信息技术", "劳动技术", "通用技术" } 
                }
            };
        }

        // 根据科目名称获取对应的科目组
        public string? GetSubjectGroupBySubject(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject)) return null;
            
            var groups = Load();
            foreach (var group in groups)
            {
                if (group.Subjects.Any(s => s.Equals(subject, StringComparison.OrdinalIgnoreCase)))
                {
                    return group.Name;
                }
            }
            
            return null;
        }

        // 获取科目组下的所有科目
        public List<string> GetSubjectsByGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return new List<string>();
            
            var groups = Load();
            var group = groups.FirstOrDefault(g => g.Name?.Equals(groupName, StringComparison.OrdinalIgnoreCase) == true);
            return group?.Subjects ?? new List<string>();
        }
    }
}
