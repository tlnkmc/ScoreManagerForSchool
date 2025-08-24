using System.Collections.Generic;
using System.IO;
using ScoreManagerForSchool.Core.Storage;
using ScoreManagerForSchool.UI.ViewModels;
using Xunit;

namespace ScoreManagerForSchool.Tests
{
    public class StudentsSearchTests
    {
        private string PrepareBaseDir()
        {
            var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "search_test");
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
            Directory.CreateDirectory(baseDir);
            return baseDir;
        }

        [Fact]
        public void SearchByPinyinFullWorks()
        {
            var baseDir = PrepareBaseDir();
            var vm = new StudentsViewModel(baseDir);
            
            // 添加包含中文姓名的学生
            var students = new List<Student>
            {
                new Student { Class = "1班", Id = "1001", Name = "张三", NamePinyin = "zhangsan", NamePinyinInitials = "zs" },
                new Student { Class = "1班", Id = "1002", Name = "李四", NamePinyin = "lisi", NamePinyinInitials = "ls" },
                new Student { Class = "2班", Id = "2001", Name = "王五", NamePinyin = "wangwu", NamePinyinInitials = "ww" }
            };
            vm.ApplyImported(students);

            // 测试全拼搜索
            vm.Query = "zhangsan";
            Assert.Single(vm.Students);
            Assert.Equal("张三", vm.Students[0].Name);

            // 测试部分全拼搜索
            vm.Query = "zhang";
            Assert.Single(vm.Students);
            Assert.Equal("张三", vm.Students[0].Name);
        }

        [Fact]
        public void SearchByPinyinInitialsWorks()
        {
            var baseDir = PrepareBaseDir();
            var vm = new StudentsViewModel(baseDir);
            
            var students = new List<Student>
            {
                new Student { Class = "1班", Id = "1001", Name = "张三", NamePinyin = "zhangsan", NamePinyinInitials = "zs" },
                new Student { Class = "1班", Id = "1002", Name = "赵四", NamePinyin = "zhaosi", NamePinyinInitials = "zs" },
                new Student { Class = "2班", Id = "2001", Name = "李五", NamePinyin = "liwu", NamePinyinInitials = "lw" }
            };
            vm.ApplyImported(students);

            // 测试首字母搜索 - 应该匹配张三和赵四（都是zs）
            vm.Query = "zs";
            Assert.Equal(2, vm.Students.Count);
            Assert.Contains(vm.Students, s => s.Name == "张三");
            Assert.Contains(vm.Students, s => s.Name == "赵四");

            // 测试单个首字母
            vm.Query = "z";
            Assert.Equal(2, vm.Students.Count);
        }

        [Fact]
        public void SearchMixedChineseAndPinyinWorks()
        {
            var baseDir = PrepareBaseDir();
            var vm = new StudentsViewModel(baseDir);
            
            var students = new List<Student>
            {
                new Student { Class = "1班", Id = "1001", Name = "张三", NamePinyin = "zhangsan", NamePinyinInitials = "zs" },
                new Student { Class = "1班", Id = "1002", Name = "李四", NamePinyin = "lisi", NamePinyinInitials = "ls" },
                new Student { Class = "2班", Id = "2001", Name = "Alice", NamePinyin = "alice", NamePinyinInitials = "a" }
            };
            vm.ApplyImported(students);

            // 测试中文姓名搜索仍然有效
            vm.Query = "张";
            Assert.Single(vm.Students);
            Assert.Equal("张三", vm.Students[0].Name);

            // 测试英文姓名的拼音搜索
            vm.Query = "alice";
            Assert.Single(vm.Students);
            Assert.Equal("Alice", vm.Students[0].Name);

            // 测试班级搜索仍然有效
            vm.Query = "1班";
            Assert.Equal(2, vm.Students.Count);
        }

        [Fact]
        public void NonAsciiSearchDoesNotUsePinyin()
        {
            var baseDir = PrepareBaseDir();
            var vm = new StudentsViewModel(baseDir);
            
            var students = new List<Student>
            {
                new Student { Class = "1班", Id = "1001", Name = "张三", NamePinyin = "zhangsan", NamePinyinInitials = "zs" }
            };
            vm.ApplyImported(students);

            // 使用中文搜索时不应该触发拼音匹配逻辑，但仍能通过姓名直接匹配
            vm.Query = "张三";
            Assert.Single(vm.Students);
            Assert.Equal("张三", vm.Students[0].Name);
        }
    }
}
