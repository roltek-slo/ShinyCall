using Microsoft.Win32;
using Squirrel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShinyCall.Services
{
    public static class UpdateManagerExtensions
    {
        private static RegistryKey OpenRunAtWindowsStartupRegistryKey() =>
            Registry.CurrentUser.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        public static void CreateRunAtWindowsStartupRegistry(this UpdateManager updateManager)
        {
            using (var startupRegistryKey = OpenRunAtWindowsStartupRegistryKey())
                startupRegistryKey.SetValue(
                    updateManager.ApplicationName,
                    Path.Combine(updateManager.RootAppDirectory, $"{updateManager.ApplicationName}.exe"));
        }

        public static void RemoveRunAtWindowsStartupRegistry(this UpdateManager updateManager)
        {
            using (var startupRegistryKey = OpenRunAtWindowsStartupRegistryKey())
                startupRegistryKey.DeleteValue(updateManager.ApplicationName);
        }
    }
}
