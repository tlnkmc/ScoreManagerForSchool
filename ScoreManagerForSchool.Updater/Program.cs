using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Formats.Tar;

internal class Program
{
    static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: update -u --token <ticks> | -uf");
                return 1;
            }

            if (args[0] == "-u")
            {
                // token validation
                long ticks = 0;
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i] == "--token" && long.TryParse(args[i + 1], out ticks)) break;
                }
                if (ticks == 0) { Console.WriteLine("Missing token"); return 2; }
                var sent = new DateTime(ticks, DateTimeKind.Utc);
                var now = DateTime.UtcNow;
                if (Math.Abs((now - sent).TotalSeconds) > 30)
                {
                    Console.WriteLine("Token expired");
                    return 3;
                }

                // Find update package in current directory
                var cwd = Directory.GetCurrentDirectory();
                var pkg = FindUpdatePackage(cwd);
                if (pkg == null)
                {
                    Console.WriteLine("No update package found");
                    return 4;
                }

                var updDir = Path.Combine(cwd, "update");
                if (Directory.Exists(updDir)) TryDeleteDirectory(updDir);
                Directory.CreateDirectory(updDir);

                ExtractPackage(pkg, updDir);

                // Run update.exe -uf from within update directory
                var updaterPath = ResolveUpdaterExecutable(updDir);
                if (!File.Exists(updaterPath))
                {
                    // If not found, try run current executable with -uf from the update folder
                    updaterPath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    var psiFallback = new ProcessStartInfo(updaterPath, "-uf") { WorkingDirectory = updDir, UseShellExecute = false };
                    Process.Start(psiFallback);
                }
                else
                {
                    var psi = new ProcessStartInfo(updaterPath, "-uf") { WorkingDirectory = Path.GetDirectoryName(updaterPath)!, UseShellExecute = false };
                    Process.Start(psi);
                }
                return 0;
            }
            else if (args[0] == "-uf")
            {
                var cwd = Directory.GetCurrentDirectory();
                var updDir = Path.Combine(cwd, "update");

                // Delete everything except base and update
                foreach (var dir in Directory.GetDirectories(cwd))
                {
                    var name = Path.GetFileName(dir).ToLowerInvariant();
                    if (name is "base" or "update") continue;
                    TryDeleteDirectory(dir);
                }
                foreach (var file in Directory.GetFiles(cwd))
                {
                    var name = Path.GetFileName(file).ToLowerInvariant();
                    if (name is "update" or "update.zip" or "update.tar.gz") { try { File.Delete(file); } catch { } continue; }
                    try { File.Delete(file); } catch { }
                }
                // Copy update content over
                CopyDirectory(updDir, cwd);

                // Start main app and exit
                var exeName = ResolveMainExecutableName();
                var exePath = Path.Combine(cwd, exeName);
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
                    else
                    {
                        // Ensure executable bit on Unix
                        try { Process.Start("chmod", $"+x \"{exePath}\""); } catch { }
                        Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = false });
                    }
                }
                catch { }
                return 0;
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return -1;
        }
    }

    static string? FindUpdatePackage(string cwd)
    {
        var candidates = new[] { "update", "update.zip", "update.tar.gz" };
        foreach (var name in candidates)
        {
            var p = Path.Combine(cwd, name);
            if (File.Exists(p)) return p;
        }
        // If not found, search for latest file matching pattern
        foreach (var f in Directory.GetFiles(cwd))
        {
            var ext = Path.GetExtension(f).ToLowerInvariant();
            if (ext is ".zip" || f.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)) return f;
        }
        return null;
    }

    static void ExtractPackage(string pkgPath, string destDir)
    {
        // Detect by magic header
        using var fs = File.OpenRead(pkgPath);
        int b1 = fs.ReadByte();
        int b2 = fs.ReadByte();
        fs.Position = 0;
        if (b1 == 'P' && b2 == 'K')
        {
            ZipFile.ExtractToDirectory(fs, destDir, overwriteFiles: true);
        }
        else if (b1 == 0x1F && b2 == 0x8B)
        {
            using var gz = new System.IO.Compression.GZipStream(fs, CompressionMode.Decompress, leaveOpen: false);
            TarFile.ExtractToDirectory(gz, destDir, overwriteFiles: true);
        }
        else
        {
            // Fallback try zip by path
            ZipFile.ExtractToDirectory(pkgPath, destDir, overwriteFiles: true);
        }
    }

    static string ResolveUpdaterExecutable(string updDir)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Path.Combine(updDir, "update.exe");
        return Path.Combine(updDir, "update");
    }

    static string ResolveMainExecutableName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "scoremgr.exe";
        return "scoremgr";
    }

    static void TryDeleteDirectory(string dir)
    {
        try
        {
            DirectoryInfo di = new DirectoryInfo(dir) { Attributes = FileAttributes.Normal };
            foreach (var info in di.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
            {
                if (info is FileInfo fi)
                {
                    fi.Attributes = FileAttributes.Normal;
                    fi.Delete();
                }
                else if (info is DirectoryInfo dd)
                {
                    dd.Attributes = FileAttributes.Normal;
                }
            }
            di.Delete(true);
        }
        catch { }
    }

    static void CopyDirectory(string src, string dst)
    {
        foreach (var dirPath in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(src, dst));
        }
        foreach (var filePath in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
        {
            var destFile = filePath.Replace(src, dst);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            File.Copy(filePath, destFile, true);
        }
    }
}
