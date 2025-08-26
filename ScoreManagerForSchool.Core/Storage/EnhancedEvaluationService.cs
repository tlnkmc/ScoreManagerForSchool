using System;
using System.Collections.Generic;
using System.Linq;

namespace ScoreManagerForSchool.Core.Storage
{
    public class EnhancedEvaluationService
    {
        private readonly EvaluationStore _evaluationStore;
        private readonly TeacherStore _teacherStore;
        private readonly SubjectGroupStore _subjectGroupStore;
        private readonly StudentStore _studentStore;
        private readonly TeacherEvaluationStore _teacherEvaluationStore;

        public EnhancedEvaluationService(string baseDir)
        {
            _evaluationStore = new EvaluationStore(baseDir);
            _teacherStore = new TeacherStore(baseDir);
            _subjectGroupStore = new SubjectGroupStore(baseDir);
            _studentStore = new StudentStore(baseDir);
            _teacherEvaluationStore = new TeacherEvaluationStore(baseDir);
        }

        // 添加积分记录（同时记录教师和科目组）
        public void AddEvaluation(string className, string studentId, string studentName, 
            string item, double score, string? remark = null, 
            string? teacherQuery = null)
        {
            var evaluation = new EvaluationEntry
            {
                Class = className,
                StudentId = studentId,
                Name = studentName,
                Date = DateTime.Now, // 本地当前时间
                Item = item,
                Score = score,
                Remark = remark
            };

            Teacher? matchedTeacher = null;

            // 如果提供了教师查询，匹配教师信息
            if (!string.IsNullOrWhiteSpace(teacherQuery))
            {
                // 先尝试科目匹配
                var detectedSubject = DetectSubjectFromInput(teacherQuery);
                if (!string.IsNullOrWhiteSpace(detectedSubject))
                {
                    matchedTeacher = MatchTeacherBySubjectAndClass(detectedSubject, className);
                }

                // 如果科目匹配失败，回退到姓名匹配
                if (matchedTeacher == null)
                {
                    matchedTeacher = MatchTeacher(teacherQuery, className);
                }

                if (matchedTeacher != null)
                {
                    evaluation.TeacherName = matchedTeacher.Name;
                    evaluation.Subject = matchedTeacher.Subject;
                    evaluation.SubjectGroup = matchedTeacher.SubjectGroup;
                }
            }

            // 如果没有匹配到教师但有科目信息，尝试推断科目组
            if (string.IsNullOrEmpty(evaluation.SubjectGroup) && !string.IsNullOrEmpty(evaluation.Subject))
            {
                evaluation.SubjectGroup = _subjectGroupStore.GetSubjectGroupBySubject(evaluation.Subject);
            }

            // 保存学生积分记录
            var evaluations = _evaluationStore.Load();
            evaluations.Add(evaluation);
            _evaluationStore.Save(evaluations);

            // 同时为教师记录积分（如果匹配到教师）
            if (matchedTeacher != null)
            {
                AddTeacherEvaluation(matchedTeacher, className, item, score, remark, studentId, studentName);
            }
        }

        // 为教师添加积分记录
        public void AddTeacherEvaluation(Teacher teacher, string className, string item, double score, 
            string? remark = null, string? relatedStudentId = null, string? relatedStudentName = null)
        {
            var teacherEvaluation = new TeacherEvaluationEntry
            {
                TeacherName = teacher.Name,
                Subject = teacher.Subject,
                SubjectGroup = teacher.SubjectGroup,
                Class = className,
                Date = DateTime.Now, // 本地当前时间
                Item = item,
                Score = score,
                Remark = remark,
                RelatedStudentId = relatedStudentId,
                RelatedStudentName = relatedStudentName
            };

            _teacherEvaluationStore.Add(teacherEvaluation);
        }

        // 从输入文本中检测科目关键词
        private string? DetectSubjectFromInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            // 科目关键词映射表
            var subjectKeywords = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["语文"] = new List<string> { "语文", "chinese", "yw", "yuwen" },
                ["数学"] = new List<string> { "数学", "math", "mathematics", "sx", "shuxue" },
                ["英语"] = new List<string> { "英语", "english", "yy", "yingyu" },
                ["物理"] = new List<string> { "物理", "physics", "wl", "wuli" },
                ["化学"] = new List<string> { "化学", "chemistry", "hx", "huaxue" },
                ["生物"] = new List<string> { "生物", "biology", "sw", "shengwu" },
                ["政治"] = new List<string> { "政治", "politics", "zz", "zhengzhi" },
                ["历史"] = new List<string> { "历史", "history", "ls", "lishi" },
                ["地理"] = new List<string> { "地理", "geography", "dl", "dili" },
                ["体育"] = new List<string> { "体育", "pe", "sports", "ty", "tiyu" },
                ["音乐"] = new List<string> { "音乐", "music", "yl", "yinyue" },
                ["美术"] = new List<string> { "美术", "art", "ms", "meishu" },
                ["信息技术"] = new List<string> { "信息技术", "计算机", "computer", "it", "信息", "xxjs", "jisuan" },
                ["科学"] = new List<string> { "科学", "science", "kx", "kexue" }
            };

            // 检查每个科目的关键词
            foreach (var subject in subjectKeywords)
            {
                foreach (var keyword in subject.Value)
                {
                    if (input.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        return subject.Key;
                    }
                }
            }

            return null;
        }

        // 匹配教师（支持姓名和拼音搜索）
        public Teacher? MatchTeacher(string query, string? className = null)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;

            var teachers = _teacherStore.SearchByName(query);
            
            // 如果指定了班级，优先返回任教该班级的教师
            if (!string.IsNullOrWhiteSpace(className))
            {
                var classTeacher = teachers.FirstOrDefault(t => t.Classes.Contains(className));
                if (classTeacher != null) return classTeacher;
            }

            // 否则返回第一个匹配的教师
            return teachers.FirstOrDefault();
        }

        // 按科目和班级匹配教师
        public Teacher? MatchTeacherBySubjectAndClass(string subject, string? className = null)
        {
            if (string.IsNullOrWhiteSpace(subject)) return null;

            var allTeachers = _teacherStore.Load();
            
            // 按科目匹配教师
            var subjectTeachers = allTeachers.Where(t => 
                !string.IsNullOrEmpty(t.Subject) && 
                (t.Subject.Contains(subject, StringComparison.OrdinalIgnoreCase) ||
                 (!string.IsNullOrEmpty(t.SubjectGroup) && t.SubjectGroup.Contains(subject, StringComparison.OrdinalIgnoreCase)))
            ).ToList();

            if (subjectTeachers.Count == 0) return null;

            // 如果指定了班级，优先返回任教该班级的教师
            if (!string.IsNullOrWhiteSpace(className))
            {
                var classTeacher = subjectTeachers.FirstOrDefault(t => 
                    t.Classes.Any(c => string.Equals(c, className, StringComparison.OrdinalIgnoreCase)));
                if (classTeacher != null) return classTeacher;
            }

            // 否则返回第一个匹配的科目教师
            return subjectTeachers.FirstOrDefault();
        }

        // 获取班级的任课教师列表
        public List<Teacher> GetClassTeachers(string className)
        {
            return _teacherStore.GetTeachersByClass(className);
        }

        // 获取科目组统计
        public Dictionary<string, double> GetSubjectGroupScores(string className, DateTime? startDate = null, DateTime? endDate = null)
        {
            var evaluations = _evaluationStore.Load();
            var query = evaluations.Where(e => e.Class == className);

            if (startDate.HasValue)
                query = query.Where(e => e.Date >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(e => e.Date <= endDate.Value);

            return query
                .Where(e => !string.IsNullOrEmpty(e.SubjectGroup))
                .GroupBy(e => e.SubjectGroup!)
                .ToDictionary(g => g.Key, g => g.Sum(e => e.Score));
        }

        // 获取教师积分统计
        public Dictionary<string, double> GetTeacherScores(string className, DateTime? startDate = null, DateTime? endDate = null)
        {
            var evaluations = _evaluationStore.Load();
            var query = evaluations.Where(e => e.Class == className);

            if (startDate.HasValue)
                query = query.Where(e => e.Date >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(e => e.Date <= endDate.Value);

            return query
                .Where(e => !string.IsNullOrEmpty(e.TeacherName))
                .GroupBy(e => e.TeacherName!)
                .ToDictionary(g => g.Key, g => g.Sum(e => e.Score));
        }

        // 搜索教师（支持拼音）
        public List<Teacher> SearchTeachers(string query)
        {
            return _teacherStore.SearchByName(query);
        }

        // 获取所有科目组
        public List<SubjectGroup> GetSubjectGroups()
        {
            return _subjectGroupStore.Load();
        }

        // 批量导入教师
        public void ImportTeachers(List<Teacher> teachers)
        {
            var existingTeachers = _teacherStore.Load();
            var allTeachers = new List<Teacher>(existingTeachers);

            foreach (var newTeacher in teachers)
            {
                // 检查是否已存在（按姓名）
                var existing = allTeachers.FirstOrDefault(t => t.Name == newTeacher.Name);
                if (existing != null)
                {
                    // 更新现有教师信息
                    existing.Subject = newTeacher.Subject;
                    existing.SubjectGroup = newTeacher.SubjectGroup;
                    existing.Classes = newTeacher.Classes;
                    existing.NamePinyin = newTeacher.NamePinyin;
                    existing.NamePinyinInitials = newTeacher.NamePinyinInitials;
                }
                else
                {
                    // 添加新教师
                    allTeachers.Add(newTeacher);
                }
            }

            _teacherStore.Save(allTeachers);
        }
    }
}
