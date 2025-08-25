using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ScoreManagerForSchool.Core.Storage
{
    /// <summary>
    /// 数据迁移助手，用于将JSON文件迁移到加密数据库
    /// </summary>
    public static class DataMigrationHelper
    {
        /// <summary>
        /// 迁移指定类型的数据从JSON到加密数据库
        /// </summary>
        public static bool MigrateData<T>(string baseDir, string jsonFileName, string encryptedFileName)
        {
            try
            {
                var jsonPath = Path.Combine(baseDir, jsonFileName);
                if (!File.Exists(jsonPath)) return false;

                // 读取JSON数据
                var jsonContent = File.ReadAllText(jsonPath);
                if (string.IsNullOrWhiteSpace(jsonContent)) return false;

                // 使用一致的JSON选项进行反序列化
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var data = JsonSerializer.Deserialize<T>(jsonContent, options);
                if (data == null) return false;

                // 保存到加密数据库
                var encryptedStore = new EncryptedDataStore<T>(baseDir, encryptedFileName);
                encryptedStore.Save(data);

                // 备份原JSON文件
                var backupPath = jsonPath + ".backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.Move(jsonPath, backupPath);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 执行所有数据的迁移
        /// </summary>
        public static void MigrateAllData(string baseDir)
        {
            try
            {
                Directory.CreateDirectory(baseDir);

                // 迁移学生数据
                MigrateData<List<Student>>(baseDir, "students.json", "students");

                // 迁移班级数据
                MigrateData<List<ClassInfo>>(baseDir, "classes.json", "classes");

                // 迁移评价方案数据
                MigrateData<List<object>>(baseDir, "schemes.json", "schemes");

                // 迁移评价记录数据
                MigrateData<List<EvaluationEntry>>(baseDir, "evaluations.json", "evaluations");

                // 迁移教师数据
                MigrateData<List<Teacher>>(baseDir, "teachers.json", "teachers");

                // 迁移科目组数据
                MigrateData<List<SubjectGroup>>(baseDir, "subjectgroups.json", "subjectgroups");

                // 迁移安全问答数据
                MigrateData<List<object>>(baseDir, "secqa.json", "secqa");

                // 迁移应用配置数据
                MigrateData<object>(baseDir, "appconfig.json", "appconfig");

                Console.WriteLine("数据迁移完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"数据迁移失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查是否需要进行数据迁移
        /// </summary>
        public static bool NeedsMigration(string baseDir)
        {
            try
            {
                var jsonFiles = new[]
                {
                    "students.json", "classes.json", "schemes.json", "evaluations.json",
                    "teachers.json", "subjectgroups.json", "secqa.json", "appconfig.json"
                };

                foreach (var jsonFile in jsonFiles)
                {
                    var jsonPath = Path.Combine(baseDir, jsonFile);
                    if (File.Exists(jsonPath))
                    {
                        return true; // 发现JSON文件，需要迁移
                    }
                }

                return false; // 没有发现JSON文件，不需要迁移
            }
            catch
            {
                return false;
            }
        }
    }
}
