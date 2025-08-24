using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ScoreManagerForSchool.Core.Storage
{
    public class TeacherStore
    {
        private readonly string _path;

        public TeacherStore(string baseDir)
        {
            Directory.CreateDirectory(baseDir);
            _path = Path.Combine(baseDir, "teachers.json");
        }

        public List<Teacher> Load()
        {
            if (!File.Exists(_path)) return new List<Teacher>();
            
            try
            {
                var teachers = JsonSerializer.Deserialize<List<Teacher>>(File.ReadAllText(_path)) ?? new List<Teacher>();
                
                // 为现有教师生成拼音（如果缺失）
                bool hasUpdates = false;
                foreach (var teacher in teachers)
                {
                    if (!string.IsNullOrEmpty(teacher.Name) && string.IsNullOrEmpty(teacher.NamePinyin))
                    {
                        var (pinyin, initials) = PinyinUtil.MakeKeys(teacher.Name);
                        teacher.NamePinyin = pinyin;
                        teacher.NamePinyinInitials = initials;
                        hasUpdates = true;
                    }
                }
                
                if (hasUpdates)
                {
                    Save(teachers);
                }
                
                return teachers;
            }
            catch
            {
                return new List<Teacher>();
            }
        }

        public void Save(IEnumerable<Teacher> teachers)
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(teachers, new JsonSerializerOptions { WriteIndented = true }));
        }

        // 根据姓名或拼音搜索教师
        public List<Teacher> SearchByName(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<Teacher>();
            
            var teachers = Load();
            var lowerQuery = query.ToLowerInvariant();
            
            return teachers.Where(t => 
                (!string.IsNullOrEmpty(t.Name) && t.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(t.NamePinyin) && t.NamePinyin.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(t.NamePinyinInitials) && t.NamePinyinInitials.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        // 根据班级查找任课教师
        public List<Teacher> GetTeachersByClass(string className)
        {
            if (string.IsNullOrWhiteSpace(className)) return new List<Teacher>();
            
            var teachers = Load();
            return teachers.Where(t => t.Classes.Contains(className)).ToList();
        }

        // 根据科目组查找教师
        public List<Teacher> GetTeachersBySubjectGroup(string subjectGroup)
        {
            if (string.IsNullOrWhiteSpace(subjectGroup)) return new List<Teacher>();
            
            var teachers = Load();
            return teachers.Where(t => !string.IsNullOrEmpty(t.SubjectGroup) && 
                t.SubjectGroup.Equals(subjectGroup, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }
}
