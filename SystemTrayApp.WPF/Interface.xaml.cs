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
using Newtonsoft.Json;

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
      
    


   



        private async void Interface_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {          
                using (var mgr = await UpdateManager.GitHubUpdateManager("https://github.com/CodingByDay/shiny-call"))
                {
                    updateManager = mgr;
                    var release = await mgr.UpdateApp();
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
              
            } finally
            {
                UpdateConfig();
                // Testing
            }
        }
   
        private void UpdateConfig()
        {
            try
            {
                string myTempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "settings.txt");
                string text = File.ReadAllText(myTempFile, Encoding.UTF8);

                if(text.Length < 10) { return; }
                
                dynamic? obj = JsonConvert.DeserializeObject(text);
                System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                string password = obj.password;
                string server = obj.server;
                string username = obj.username;
                string phone_number = obj.phone_number;
                string api = obj.api;
                string port = obj.port;
                string id = obj.user_data;
                string contact = obj.contact;


                config.AppSettings.Settings["SIPPassword"].Value = password;
                config.AppSettings.Settings["SIPServer"].Value = server;
                config.AppSettings.Settings["SIPUsername"].Value = username;
                config.AppSettings.Settings["SIPPhoneNumber"].Value = phone_number;
                config.AppSettings.Settings["APIaddress"].Value = api;
                config.AppSettings.Settings["SIPport"].Value = port;
                config.AppSettings.Settings["UserData"].Value = id;
                config.AppSettings.Settings["contact"].Value = contact;

                config.Save(ConfigurationSaveMode.Modified);

                ConfigurationManager.RefreshSection("appSettings");
            } catch {
                return;
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
