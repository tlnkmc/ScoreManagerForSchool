using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ScoreManagerForSchool.UI.Services
{
    public static class AutostartManager
    {
        private const string AppName = "ScoreManagerForSchool";

        public static bool IsSupported => OperatingSystem.IsWindows();

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public static bool GetEnabled()
        {
            if (!IsSupported) return false;
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", false);
                var val = key?.GetValue(AppName) as string;
                return !string.IsNullOrEmpty(val);
            }
            catch { return false; }
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public static void SetEnabled(bool enabled)
        {
            if (!IsSupported) return;
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (key == null) return;
                if (enabled)
                {
                    var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppName, '"' + exePath + '"');
                    }
                }
                else
                {
                    try { key.DeleteValue(AppName, false); } catch { }
                }
            }
            catch { }
        }
    }
}
