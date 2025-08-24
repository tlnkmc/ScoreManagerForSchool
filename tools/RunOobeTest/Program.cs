using ScoreManagerForSchool.UI.ViewModels;
using System;
using System.IO;

class Program
{
    static int Main()
    {
    var repoRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".."));
    var vm = new OobeViewModel();
    vm.StudentsPath = Path.Combine(repoRoot, "testdata", "students.csv");
    vm.ClassesPath = Path.Combine(repoRoot, "testdata", "classes.csv");
    vm.SchemesPath = Path.Combine(repoRoot, "testdata", "schemes.csv");
        var user = "UserPass123!".AsSpan();
        var userConf = "UserPass123!".AsSpan();
        var second = "SecondPass456!".AsSpan();
        var secondConf = "SecondPass456!".AsSpan();
        var ok = vm.SaveAndImport(user, userConf, second, secondConf);
        Console.WriteLine(ok ? "SaveAndImport OK" : "SaveAndImport FAILED");
        return ok ? 0 : 1;
    }
}
