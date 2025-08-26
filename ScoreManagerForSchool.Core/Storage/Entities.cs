using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ScoreManagerForSchool.Core.Storage
{
    public class Student
    {
        public string? Class { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
    // 隐藏字段：用于姓名拼音匹配（不可见，仅供检索加速）
    public string? NamePinyin { get; set; }            // 全拼，letters-only lower，例如 "zhangsan"
    public string? NamePinyinInitials { get; set; }    // 首字母，例如 "zs"
    }

    public class ClassInfo
    {
        public string? Class { get; set; }
        public string? Type { get; set; }
    }

    // 教师信息
    public class Teacher
    {
        public string? Name { get; set; }         // 教师姓名
        public string? Subject { get; set; }     // 任教科目
        public string? SubjectGroup { get; set; } // 科目组（语文、数学等）
        public List<string> Classes { get; set; } = new List<string>(); // 任教班级列表
        
        // 隐藏字段：用于教师姓名拼音匹配
        public string? NamePinyin { get; set; }            // 全拼，letters-only lower
        public string? NamePinyinInitials { get; set; }    // 首字母

        // 用于UI显示的班级文本
        public string ClassesText => Classes != null ? string.Join(", ", Classes) : string.Empty;
    }

    // 科目组信息
    public class SubjectGroup
    {
        public string? Name { get; set; }         // 科目组名称（语文、数学、英语等）
        public string? Description { get; set; } // 描述
        public List<string> Subjects { get; set; } = new List<string>(); // 包含的科目列表
    }

    // 教师积分记录
    public class TeacherEvaluationEntry
    {
        public string? Id { get; set; }            // 唯一标识
        public string? TeacherId { get; set; }     // 教师ID
        public string? TeacherName { get; set; }   // 教师姓名
        public string? Subject { get; set; }       // 科目
        public string? SubjectGroup { get; set; }  // 科目组
        public string? Class { get; set; }         // 相关班级
    [JsonConverter(typeof(DateTimeJsonConverter))]
    public DateTime Date { get; set; } = DateTime.Now;  // 记录时间（使用本地时间存储）
        public string? Item { get; set; }          // 评价项目
        public double Score { get; set; }          // 积分
        public string? Remark { get; set; }        // 备注
        public string? RelatedStudentId { get; set; }  // 相关学生ID（如果有）
        public string? RelatedStudentName { get; set; } // 相关学生姓名（如果有）
    }

    // 班级详细信息（扩展原有ClassInfo）
    public class ClassDetail
    {
        public string? Class { get; set; }        // 班级名称
        public string? Type { get; set; }         // 班级类型
        public string? Grade { get; set; }        // 年级
        public List<string> Teachers { get; set; } = new List<string>(); // 任课教师ID列表
        public int StudentCount { get; set; }     // 学生人数
    }
}
