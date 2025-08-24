using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ScoreManagerForSchool.Core.Storage;
using ScoreManagerForSchool.UI.ViewModels;
using Xunit;

namespace ScoreManagerForSchool.Tests
{
    public class ExportTests
    {
        private string PrepareBaseDir()
        {
            var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "export_test");
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
            Directory.CreateDirectory(baseDir);
            return baseDir;
        }

        [Fact]
        public void ExportWithTimeRangeFiltersCorrectly()
        {
            var baseDir = PrepareBaseDir();
            
            // 准备测试数据
            var students = new List<Student>
            {
                new Student { Class = "1班", Id = "1001", Name = "张三", NamePinyin = "zhangsan", NamePinyinInitials = "zs" },
                new Student { Class = "1班", Id = "1002", Name = "李四", NamePinyin = "lisi", NamePinyinInitials = "ls" }
            };
            new StudentStore(baseDir).Save(students);

            var evaluations = new List<EvaluationEntry>
            {
                new EvaluationEntry { Class = "1班", Name = "张三", StudentId = "1001", Score = 10, Date = DateTime.Today.AddDays(-2), Remark = "老数据" },
                new EvaluationEntry { Class = "1班", Name = "张三", StudentId = "1001", Score = 5, Date = DateTime.Today, Remark = "今天数据" },
                new EvaluationEntry { Class = "1班", Name = "李四", StudentId = "1002", Score = 8, Date = DateTime.Today.AddHours(1), Remark = "今天数据2" }
            };
            new EvaluationStore(baseDir).Save(evaluations);

            var vm = new StatsViewModel(baseDir);
            
            // 模拟导出对话框返回"今天"的时间范围
            var startDate = DateTime.Today;
            var endDate = DateTime.Today.AddDays(1).AddSeconds(-1);
            
            // 使用反射调用私有的导出方法进行测试
            var method = typeof(StatsViewModel).GetMethod("ExportCsv", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null, new[] { typeof(DateTime), typeof(DateTime) }, null);
            
            Assert.NotNull(method);
            method.Invoke(vm, new object[] { startDate, endDate });

            // 验证导出文件存在
            var exportDir = Path.Combine(baseDir, "export");
            Assert.True(Directory.Exists(exportDir));
            
            var files = Directory.GetFiles(exportDir, "scores-*.csv");
            Assert.Single(files);
            
            var content = File.ReadAllText(files[0]);
            
            // 验证只包含今天的数据
            Assert.Contains("今天数据", content);
            Assert.Contains("今天数据2", content);
            Assert.DoesNotContain("老数据", content);
            
            // 验证包含统计汇总
            Assert.Contains("=== 统计汇总 ===", content);
            Assert.Contains("张三", content);
            Assert.Contains("李四", content);
        }

        [Fact]
        public void ExportDialogViewModelDateRangeWorks()
        {
            var vm = new ScoreManagerForSchool.UI.Views.ExportDialogViewModel();
            
            // 测试今天范围
            vm.SelectedRange = ScoreManagerForSchool.UI.Views.ExportTimeRange.Today;
            Assert.Equal(DateTime.Today, vm.StartDate);
            Assert.True(vm.EndDate >= DateTime.Today.AddDays(1).AddSeconds(-2)); // 允许一些时间误差
            Assert.Contains("今天", vm.RangeDescription);
            
            // 测试昨天范围
            vm.SelectedRange = ScoreManagerForSchool.UI.Views.ExportTimeRange.Yesterday;
            Assert.Equal(DateTime.Today.AddDays(-1), vm.StartDate);
            Assert.True(vm.EndDate >= DateTime.Today.AddSeconds(-2));
            Assert.Contains("昨天", vm.RangeDescription);
            
            // 测试本周范围
            vm.SelectedRange = ScoreManagerForSchool.UI.Views.ExportTimeRange.ThisWeek;
            var today = DateTime.Today;
            var daysFromMonday = (int)today.DayOfWeek - 1;
            if (daysFromMonday < 0) daysFromMonday = 6;
            var expectedStart = today.AddDays(-daysFromMonday);
            Assert.Equal(expectedStart, vm.StartDate);
            Assert.Contains("本周", vm.RangeDescription);
            
            // 测试自定义范围
            vm.SelectedRange = ScoreManagerForSchool.UI.Views.ExportTimeRange.Custom;
            Assert.True(vm.IsCustomDateVisible);
            Assert.Contains("自定义", vm.RangeDescription);
        }
    }
}
