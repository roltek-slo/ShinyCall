using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using WPFNotification.Model;
using WPFNotification.Services;

namespace ShinyCall.Services
{
    internal static class Services
    {

        public static int duplicate = 0;
        public static string GetTheme()
        {
            string RegistryKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes";
            string? theme;
            theme = (string) Registry.GetValue(RegistryKey, "CurrentTheme", string.Empty);
            theme = theme.Split('\\').Last().Split('.').First().ToString();
            return theme;
        }
        public static string GetAppSettings(string key)
        {
            try
            {
                return ConfigurationManager.AppSettings[key];
            } catch (Exception)
            {
                return string.Empty;
            }
        }

        public static void AddUpdateAppSettings(string key, string value)
        {
            try { 
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings[key].Value = value;
            configuration.Save();

            ConfigurationManager.RefreshSection("appSettings"); 
            } catch
            {
            }
        }
   

        // using System.Net.NetworkInformation;
        public static bool IsMachineUp(string hostName)
        {
            bool retVal = false;
            try
            {
                Ping pingSender = new Ping();
                PingOptions options = new PingOptions();
                // Use the default Ttl value which is 128,
                // but change the fragmentation behavior.
                options.DontFragment = true;
                // Create a buffer of 32 bytes of data to be transmitted.
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 120;

                PingReply reply = pingSender.Send(hostName, timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    retVal = true;
                }
            }
            catch (Exception ex)
            {
                retVal = false;
                Console.WriteLine(ex.Message);
            }
            return retVal;
        }
    }
}
