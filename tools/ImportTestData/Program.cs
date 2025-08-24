using System;
using System.IO;
using ScoreManagerForSchool.Core.Storage;

var repoRoot = Directory.GetCurrentDirectory();
// adjust if run from tools folder
if (Path.GetFileName(repoRoot).Equals("ImportTestData", StringComparison.OrdinalIgnoreCase))
    repoRoot = Path.GetFullPath(Path.Combine(repoRoot, "..", ".."));

var testdata = Path.Combine(repoRoot, "testdata");
var baseDir = Path.Combine(repoRoot, "base");
Directory.CreateDirectory(baseDir);
Console.WriteLine($"Importing CSVs from: {testdata} -> {baseDir}");

var studentsCsv = Path.Combine(testdata, "students.csv");
if (File.Exists(studentsCsv))
{
    var students = CsvImporter.ImportStudents(studentsCsv);
    var sstore = new StudentStore(baseDir);
    sstore.Save(students);
    Console.WriteLine($"Imported {students.Count} students.");
}
else Console.WriteLine("students.csv not found.");

var classesCsv = Path.Combine(testdata, "classes.csv");
if (File.Exists(classesCsv))
{
    var classes = CsvImporter.ImportClasses(classesCsv);
    var cstore = new ClassStore(baseDir);
    cstore.Save(classes);
    Console.WriteLine($"Imported {classes.Count} classes.");
}
else Console.WriteLine("classes.csv not found.");

var schemesCsv = Path.Combine(testdata, "schemes.csv");
if (File.Exists(schemesCsv))
{
    var schemes = CsvImporter.ImportScheme(schemesCsv);
    var sch = new SchemeStore(baseDir);
    sch.Save(schemes);
    Console.WriteLine($"Imported {schemes.Count} schemes.");
}
else Console.WriteLine("schemes.csv not found.");

Console.WriteLine("Done.");
