using IWshRuntimeLibrary;
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
            string exe = System.Reflection.Assembly.GetEntryAssembly().Location;
            System.IO.File.Copy(exe, getShortcutPathInternal(), true);
        }

      

    }
}
