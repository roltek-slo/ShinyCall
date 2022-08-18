using AsterNET.Manager;
using ShinyCall.Sqlite;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using SIPSorcery.SoftPhone;
using SIPSorcery.Sys;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SystemTrayApp.WPF;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;

namespace ShinyCall.MVVM.View
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        private SIPTransportManager _sipTransportManager;
        private const int SIP_CLIENT_COUNT = 2;
        // The number of SIP clients (simultaneous calls) that the UI can handle.
        private const int ZINDEX_TOP = 10;
        private const int REGISTRATION_EXPIRY = 180;
        private string caller = string.Empty;
        private bool isMissedCall = true;
        private string m_sipUsername = SIPSoftPhoneState.SIPUsername;
        private string m_sipPassword = SIPSoftPhoneState.SIPPassword;
        private string m_sipServer = SIPSoftPhoneState.SIPServer;
        private bool m_useAudioScope = SIPSoftPhoneState.UseAudioScope;
        private List<SIPClient> _sipClients;
        private SoftphoneSTUNClient _stunClient;                    // STUN client to periodically check the public IP address.
        private SIPRegistrationUserAgent _sipRegistrationClient;
        private bool isSuccess;

        public SettingsView()
        {
            InitializeComponent();
            InitializeView();
        }

        private async void InitializeView()
        {
            password.Text = Services.Services.GetAppSettings("SIPPassword");
            server.Text = Services.Services.GetAppSettings("SIPServer");
            display_name.Text = Services.Services.GetAppSettings("SIPUsername");
            phone_number.Text = Services.Services.GetAppSettings("SIPPhoneNumber");
            api_data.Text = Services.Services.GetAppSettings("APIaddress");
            port_number.Text = Services.Services.GetAppSettings("SIPport");
            id_data.Text = Services.Services.GetAppSettings("UserData");
              
        }

        private void SaveData()
        {
            string phone_number_data = phone_number.Text;
            string server_data = server.Text;
            string password_data = password.Text;
            string display_data = display_name.Text;
            string api = api_data.Text;
            string id = id_data.Text;
            string port = port_number.Text;
            int port_num;
            if (IsValid(phone_number_data, "phone") && IsValid(server_data, "server") && Int32.TryParse(port, out port_num))
            {
                connStatus.Text = "     Spremenjeno!";
                Services.Services.AddUpdateAppSettings("SIPUsername", display_data);
                Services.Services.AddUpdateAppSettings("SIPServer", server_data);
                Services.Services.AddUpdateAppSettings("SIPPassword", password_data);
                Services.Services.AddUpdateAppSettings("SIPPhoneNumber", phone_number_data);
                Services.Services.AddUpdateAppSettings("SIPport", port);
                Services.Services.AddUpdateAppSettings("APIaddress", api);
                Services.Services.AddUpdateAppSettings("IdData", id);
                ConfigurationManager.RefreshSection("appSettings");
                SqliteDataAccess.DeleteHistory();
                var currentExecutablePath = Process.GetCurrentProcess().MainModule.FileName;
                Process.Start(currentExecutablePath);
                Application.Current.Shutdown();
            }
            else
            {
                MessageBox.Show("Napaka v podatkih.");
            }
        }

        private bool IsValid(string data, string type_data)
        {
            string pattern = string.Empty;
            bool isValid = false;
            switch (type_data)
            {
                case "phone":
                    try
                    {
                        int correct = Int32.Parse(data);
                        isValid = true;
                    }
                    catch (Exception)
                    {
                        isValid = false;
                    }
                    break;
                case "server":
                    if (Services.Services.IsMachineUp(data))
                    {
                        isValid = true;
                    }
                    else
                    {
                        isValid = false;
                    }
                    break;
            }
            return isValid;
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            SaveData();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }

        Notifier notifier = new Notifier(cfg =>
        {
            cfg.PositionProvider = new WindowPositionProvider(
                parentWindow: Application.Current.MainWindow,
                corner: Corner.TopRight,
                offsetX: 10,
                offsetY: 10);

            cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                notificationLifetime: TimeSpan.FromSeconds(5),
                maximumNotificationCount: MaximumNotificationCount.FromCount(0));
            cfg.DisplayOptions.Width = 200;

            cfg.Dispatcher = Application.Current.Dispatcher;
        });

        private SIPTransport sipTransport;
        private SIPNotifierClient mwiSubscriber;
        private ManagerConnection manager;

        private async void test_data_Click(object sender, RoutedEventArgs e)
        {
            // testing
            this.Visibility = Visibility.Visible;
            string password = Services.Services.GetAppSettings("SIPPassword");
            string server = Services.Services.GetAppSettings("SIPServer");
            string username = Services.Services.GetAppSettings("SIPUsername");
            string port = Services.Services.GetAppSettings("SIPport");
            id_data.Text = Services.Services.GetAppSettings("UserData");


            manager = new ManagerConnection(server, Int32.Parse(port), username, password);
      

            try
            {
                manager.Login();
                if (manager.IsConnected())
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        connStatus.Text = "     Uspešna prijava.";
                    });
                   
                }
                manager.Logoff();
            }
            catch (Exception ex)
            {
                this.Dispatcher.Invoke(() =>
                {
                    connStatus.Text = "     Neuspešna prijava.";
                });
            }

        }


      
       
       
        
    }
}