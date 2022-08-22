using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Windows.Themes;
using ShinyCall.Services;
using Squirrel;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;
using System.IO;

namespace ShinyCall
{
    /// <summary>
    /// Interaction logic for Interface.xaml
    /// </summary>
    public partial class Interface : Window
    {

        // Prep stuff needed to remove close button on window.
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        private UpdateManager manager;
        private UpdateManager updateManager;

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        // Asterix.

        void ToolWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Code to remove close box from window
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);


            // Code to put the windows to the bottom right.
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width;
            this.Top = desktopWorkingArea.Bottom - this.Height;
        }

        /// <summary>
        /// Above part of the code is there in order to remove the close button.
        /// </summary>

        public Interface()
        {
            InitializeComponent();
            Loaded += ToolWindow_Loaded;
            var theme = Services.Services.GetTheme();
            SetUpLookAndFeel(theme);
            Loaded += Interface_Loaded;
            AddVersionNumber();
            InstallMeOnStartup();
       
        }

   

        private void InstallMeOnStartup()
        {


           
            try
            {
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                Assembly curAssembly = Assembly.GetExecutingAssembly();
                string BaseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string ExeDir = System.IO.Path.Combine(BaseDir, "ShinyCall.exe");
                key.SetValue(curAssembly.GetName().Name, BaseDir);
            }
            catch
            {
            }
        }

        private void AddVersionNumber()
        {
            // Just updating the version information.
           string number = Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(0, 5);
            version.Text = "v" + number;
        }

        /// <summary>
        /// Make a backup of our settings.
        /// Used to persist settings across updates.
        /// </summary>
        public static void BackupSettings()
        {
            string settingsFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            string destination = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\last.config";
            File.Copy(settingsFile, destination, true);
        }

        /// <summary>
        /// Restore our settings backup if any.
        /// Used to persist settings across updates.
        /// </summary>
        public static void RestoreSettings()
        {
            //Restore settings after application update            
            string destFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            string sourceFile = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\last.config";
            // Check if we have settings that we need to restore
            if (!File.Exists(sourceFile))
            {
                // Nothing we need to do
                return;
            }
            // Create directory as needed
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destFile));
            }
            catch (Exception) { }

            // Copy our backup file in place 
            try
            {
                File.Copy(sourceFile, destFile, true);
            }
            catch (Exception) { }

            // Delete backup file
            try
            {
                File.Delete(sourceFile);
            }
            catch (Exception) { }

        }
        private async void Interface_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var mgr = await UpdateManager.GitHubUpdateManager("https://github.com/CodingByDay/shiny-call"))
                {
                    updateManager = mgr;
                    var release = await mgr.UpdateApp();
                    RestoreSettings();
                    SquirrelAwareApp.HandleEvents(
                    onAppUpdate: v =>
                    {
                        mgr.RemoveRunAtWindowsStartupRegistry();
                        mgr.CreateRunAtWindowsStartupRegistry();
                    },
                    onInitialInstall: v =>
                     {
                         mgr.CreateShortcutForThisExe();
                         mgr.CreateRunAtWindowsStartupRegistry();
                     },
                   onAppUninstall: v =>
                   {
                       mgr.RemoveShortcutForThisExe();
                       mgr.RemoveRunAtWindowsStartupRegistry();
                   });
                        

                }
            }
            catch (Exception ex)
            {
                string message = ex.Message + Environment.NewLine;
                if (ex.InnerException != null)
                    message += ex.InnerException.Message;
              
            }
        }

      
        private void SetUpLookAndFeel(string theme)
        {
         
        }

   

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            var state = this.WindowState;

            if (state == WindowState.Minimized)
            {
                this.Opacity = 0;
            } else
            {
                Opacity = 1;
            }

        }
        Notifier notifier = new Notifier(cfg =>
        {
            cfg.PositionProvider = new WindowPositionProvider(
                parentWindow: Application.Current.MainWindow,
                corner: Corner.TopRight,
                offsetX: 10,
                offsetY: 10);

            cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                notificationLifetime: TimeSpan.FromSeconds(3),
                maximumNotificationCount: MaximumNotificationCount.FromCount(1));
            cfg.DisplayOptions.Width = 200;

            cfg.Dispatcher = Application.Current.Dispatcher;
        });
        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string url = Services.Services.GetAppSettings("contact");
                var psi = new System.Diagnostics.ProcessStartInfo();
                psi.UseShellExecute = true;
                psi.FileName = url;
                System.Diagnostics.Process.Start(psi);
                Application.Current.MainWindow.WindowState = WindowState.Normal;
                Application.Current.MainWindow.Topmost = true;

            }
            catch
            {
                notifier.ShowError("Nepravilen URL");
          
                Application.Current.MainWindow.WindowState = WindowState.Normal;
                Application.Current.MainWindow.Topmost = true;


            }
        }      
    }
}
