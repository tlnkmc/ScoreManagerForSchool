using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExcelDataReader;
using System.Runtime.CompilerServices;

namespace ScoreManagerForSchool.Core.Storage
{
    public static class CsvImporter
    {
        private static string[][] ReadTabular(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path");
            var ext = Path.GetExtension(path ?? string.Empty).ToLowerInvariant();
            if (ext == ".csv" || string.IsNullOrEmpty(ext))
            {
                if (!File.Exists(path)) return Array.Empty<string[]>();
                var lines = File.ReadAllLines(path);
                return lines
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Select(l => (l ?? string.Empty).Split(','))
                    .ToArray();
            }
            if (ext == ".xls" || ext == ".xlsx")
            {
                return ReadExcel(path!);
            }
            throw new NotSupportedException($"不支持的文件类型: {ext}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string[][] ReadExcel(string path)
        {
            if (!File.Exists(path)) return Array.Empty<string[]>();
            try { System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); } catch { }
            using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(fs);
            var rows = new List<string[]>();
            do
            {
                while (reader.Read())
                {
                    var cells = new List<string>();
                    int fieldCount = 0;
                    try { fieldCount = reader.FieldCount; } catch { fieldCount = 0; }
                    for (int i = 0; i < fieldCount; i++)
                    {
                        try { cells.Add(reader.GetValue(i)?.ToString() ?? string.Empty); }
                        catch { cells.Add(string.Empty); }
                    }
                    if (cells.All(c => string.IsNullOrWhiteSpace(c))) continue;
                    rows.Add(cells.ToArray());
                }
            } while (reader.NextResult());
            return rows.ToArray();
        }
        // 学生名单 CSV: 班级,唯一号,姓名
    public static List<Student> ImportStudents(string path, bool firstRowIsHeader)
        {
            var list = new List<Student>();
            string[][] rows;
            try { rows = ReadTabular(path); } catch { return list; }
            int start = firstRowIsHeader ? 1 : 0;
            for (int i = start; i < rows.Length; i++)
            {
                var parts = rows[i] ?? Array.Empty<string>();
                if (parts.Length < 3) continue;
        var cls = (parts[0] ?? string.Empty).Trim();
        var id = (parts[1] ?? string.Empty).Trim();
        var name = (parts[2] ?? string.Empty).Trim();
        var (fullPy, initPy) = PinyinUtil.MakeKeys(name);
        list.Add(new Student { Class = cls, Id = id, Name = name, NamePinyin = fullPy, NamePinyinInitials = initPy });
            }
            return list;
        }

        // 兼容旧签名，默认无表头（首行为数据）
        public static List<Student> ImportStudents(string path)
            => ImportStudents(path, false);

        // 班级列表 CSV: 班级,类型
        public static List<ClassInfo> ImportClasses(string path, bool firstRowIsHeader)
        {
            var list = new List<ClassInfo>();
            string[][] rows;
            try { rows = ReadTabular(path); } catch { return list; }
            int start = firstRowIsHeader ? 1 : 0;
            for (int i = start; i < rows.Length; i++)
            {
                var parts = rows[i] ?? Array.Empty<string>();
                if (parts.Length < 2) continue;
                list.Add(new ClassInfo { Class = (parts[0] ?? string.Empty).Trim(), Type = (parts[1] ?? string.Empty).Trim() });
            }
            return list;
        }

        // 兼容旧签名，默认无表头
        public static List<ClassInfo> ImportClasses(string path)
            => ImportClasses(path, false);

        // 评价方案 CSV: 行为自由格式（首行为表头可选）
        public static List<string[]> ImportScheme(string path, bool firstRowIsHeader)
        {
            var list = new List<string[]>();
            string[][] rows;
            try { rows = ReadTabular(path); } catch { return list; }
            int start = firstRowIsHeader ? 1 : 0;
            for (int i = start; i < rows.Length; i++)
            {
                var parts = rows[i] ?? Array.Empty<string>();
                list.Add(parts);
            }
            return list;
        }

        // 兼容旧签名，默认无表头
        public static List<string[]> ImportScheme(string path)
            => ImportScheme(path, false);

        // 教师 CSV: 工号,姓名,科目,科目组,班级1;班级2;班级3（首行为表头可选）
        public static List<Teacher> ImportTeachers(string path, bool firstRowIsHeader = true)
        {
            var list = new List<Teacher>();
            string[][] rows;
            try { rows = ReadTabular(path); } catch { return list; }
            int start = firstRowIsHeader ? 1 : 0;
            
            for (int i = start; i < rows.Length; i++)
            {
                var parts = rows[i] ?? Array.Empty<string>();
                if (parts.Length < 2) continue; // 至少需要工号和姓名
                
                var teacher = new Teacher
                {
                    Id = parts.Length > 0 ? parts[0]?.Trim() : null,
                    Name = parts.Length > 1 ? parts[1]?.Trim() : null,
                    Subject = parts.Length > 2 ? parts[2]?.Trim() : null,
                    SubjectGroup = parts.Length > 3 ? parts[3]?.Trim() : null,
                };
                
                // 解析班级列表（分号分隔）
                if (parts.Length > 4 && !string.IsNullOrWhiteSpace(parts[4]))
                {
                    teacher.Classes = parts[4].Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .Select(c => c.Trim())
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .ToList();
                }
                
                // 生成拼音
                if (!string.IsNullOrEmpty(teacher.Name))
                {
                    var (pinyin, initials) = PinyinUtil.MakeKeys(teacher.Name);
                    teacher.NamePinyin = pinyin;
                    teacher.NamePinyinInitials = initials;
                }
                
                list.Add(teacher);
            }
            return list;
        }

        // 班级详细信息 CSV: 班级名称,类型,年级,学生人数（首行为表头可选）
        public static List<ClassDetail> ImportClassDetails(string path, bool firstRowIsHeader = true)
        {
            var list = new List<ClassDetail>();
            string[][] rows;
            try { rows = ReadTabular(path); } catch { return list; }
            int start = firstRowIsHeader ? 1 : 0;
            
            for (int i = start; i < rows.Length; i++)
            {
                var parts = rows[i] ?? Array.Empty<string>();
                if (parts.Length < 1) continue; // 至少需要班级名称
                
                var classDetail = new ClassDetail
                {
                    Class = parts.Length > 0 ? parts[0]?.Trim() : null,
                    Type = parts.Length > 1 ? parts[1]?.Trim() : null,
                    Grade = parts.Length > 2 ? parts[2]?.Trim() : null,
                };
                
                // 解析学生人数
                if (parts.Length > 3 && int.TryParse(parts[3]?.Trim(), out int count))
                {
                    classDetail.StudentCount = count;
                }
                
                list.Add(classDetail);
            }
            return list;
        }
    }
}
