using System.Collections.Generic;
using System.IO;
using System.Linq;
using ScoreManagerForSchool.Core.Storage;
using ScoreManagerForSchool.UI.ViewModels;
using Xunit;

namespace ScoreManagerForSchool.Tests
{
    public class StudentsViewModelTests
    {
        private string PrepareBaseDir()
        {
            var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "base_test");
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
            Directory.CreateDirectory(baseDir);
            return baseDir;
        }

        [Fact]
        public void ImportAndPagingWorks()
        {
            var baseDir = PrepareBaseDir();
            var csv = Path.Combine(baseDir, "students.csv");
            // create 60 students
            using (var sw = new StreamWriter(csv))
            {
                for (int i = 1; i <= 60; i++)
                {
                    sw.WriteLine($"Class{i%3+1},S{i:D4},Student {i}");
                }
            }

            var vm = new StudentsViewModel(baseDir);
            // temporarily change base dir via reflection (StudentStore uses current dir base)
            // to avoid heavy refactor, we will write students.json into working 'base' by invoking ApplyImported
            var imported = CsvImporter.ImportStudents(csv);
            vm.ApplyImported(new List<Student>(imported));

            // after applying, load should reflect saved data
            vm.Load();
            Assert.True(vm.TotalPages >= 3);
            Assert.Equal(25, vm.Students.Count);

            // move to page 3 and check count (60 -> page3 has 10)
            vm.Page = 3;
            Assert.Equal(10, vm.Students.Count);
        }

        [Fact]
        public void SearchFiltersResults()
        {
            var baseDir = PrepareBaseDir();
            var vm = new StudentsViewModel(baseDir);
            var list = new List<Student>
            {
                new Student{ Class="A", Id="1", Name="Alice"},
                new Student{ Class="B", Id="2", Name="Bob"},
                new Student{ Class="A", Id="3", Name="Charlie"}
            };
            vm.ApplyImported(list);
            vm.Query = "bob";
            Assert.Single(vm.Students);
            Assert.Equal("Bob", vm.Students[0].Name);
        }

        [Fact]
        public void EditAndDeleteWork()
        {
            var baseDir = PrepareBaseDir();
            var vm = new StudentsViewModel(baseDir);
            var list = new List<Student>
            {
                new Student{ Class="A", Id="1", Name="Alice"},
                new Student{ Class="B", Id="2", Name="Bob"}
            };
            vm.ApplyImported(list);
            vm.Load();
            // edit Bob -> Robert
            var bob = vm.Students.FirstOrDefault(s => s.Name == "Bob");
            Assert.NotNull(bob);
            vm.SelectedStudent = bob;
            vm.SelectedStudent.Name = "Robert";
            vm.SaveEditCommand.Execute(null);
            vm.Load();
            Assert.Contains(vm.Students, s => s.Name == "Robert");

            // delete Robert
            vm.SelectedStudent = vm.Students.First(s => s.Name == "Robert");
            vm.DeleteCommand.Execute(null);
            vm.Load();
            Assert.DoesNotContain(vm.Students, s => s.Name == "Robert");
        }
    }
}
