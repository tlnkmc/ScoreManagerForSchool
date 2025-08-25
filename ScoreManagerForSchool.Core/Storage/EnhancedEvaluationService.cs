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

        public EnhancedEvaluationService(string baseDir)
        {
            _evaluationStore = new EvaluationStore(baseDir);
            _teacherStore = new TeacherStore(baseDir);
            _subjectGroupStore = new SubjectGroupStore(baseDir);
            _studentStore = new StudentStore(baseDir);
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
                Date = DateTime.Now,
                Item = item,
                Score = score,
                Remark = remark
            };

            // 如果提供了教师查询，匹配教师信息
            if (!string.IsNullOrWhiteSpace(teacherQuery))
            {
                var matchedTeacher = MatchTeacher(teacherQuery, className);
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

            // 保存记录
            var evaluations = _evaluationStore.Load();
            evaluations.Add(evaluation);
            _evaluationStore.Save(evaluations);
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
