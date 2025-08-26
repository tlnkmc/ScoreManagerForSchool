using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ScoreManagerForSchool.Core.Tools;

namespace ScoreManagerForSchool.Core.Storage
{
    public class TeacherStore
    {
        private readonly EncryptedDataStore<List<Teacher>> _store;
        private readonly string _baseDir;

        public TeacherStore(string baseDir)
        {
            _baseDir = baseDir;
            Directory.CreateDirectory(baseDir);
            _store = new EncryptedDataStore<List<Teacher>>(baseDir, "teachers");
            
            // 检查是否需要数据迁移
            CheckAndMigrateData();
        }

        public List<Teacher> Load()
        {
            try
            {
                var teachers = _store.Load() ?? new List<Teacher>();
                
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
            _store.Save(new List<Teacher>(teachers));
        }

        private void CheckAndMigrateData()
        {
            var jsonPath = Path.Combine(_baseDir, "teachers.json");
            if (File.Exists(jsonPath) && !_store.Exists())
            {
                DataMigrationHelper.MigrateData<List<Teacher>>(_baseDir, "teachers.json", "teachers");
            }
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
