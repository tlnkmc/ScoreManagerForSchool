using System.IO;
using ScoreManagerForSchool.Core.Storage;
using Xunit;

namespace ScoreManagerForSchool.Tests
{
    public class PinyinTests
    {
        [Fact]
        public void ImportStudents_GeneratesPinyinKeys()
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "pinyin_test");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            var csv = Path.Combine(dir, "students.csv");
            File.WriteAllText(csv, "班级,唯一号,姓名\n1,1001,张三");

            var list = CsvImporter.ImportStudents(csv, true);
            Assert.Single(list);
            var s = list[0];
            Assert.Equal("张三", s.Name);
            Assert.Equal("zhangsan", s.NamePinyin);
            Assert.Equal("zs", s.NamePinyinInitials);
        }
    }
}
