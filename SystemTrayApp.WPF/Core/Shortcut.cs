using IWshRuntimeLibrary;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core
{
    public static class Shortcut
    {
        public static string getShortcutPathInternal()
        {
            string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            return System.IO.Path.Combine(startupPath, "ShinyCall.exe");
        }

        public static void createStartupShortcut()
        {          
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
             ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            rk.SetValue("ShinyCall", System.Reflection.Assembly.GetEntryAssembly().Location);
        }

      

    }
}
