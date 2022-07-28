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
            id_data.Text = Services.Services.GetAppSettings("UserData");
                _sipTransportManager = new SIPTransportManager();
                _sipClients = new List<SIPClient>();

                // If a STUN server hostname has been specified start the STUN client to lookup and periodically 
                // update the public IP address of the host machine.
                if (!SIPSoftPhoneState.STUNServerHostname.IsNullOrBlank())
                {
                    _stunClient = new SoftphoneSTUNClient(SIPSoftPhoneState.STUNServerHostname);
                    _stunClient.PublicIPAddressDetected += (ip) =>
                    {
                        SIPSoftPhoneState.PublicIPAddress = ip;
                    };
                    _stunClient.Run();
                }
                await Initialize();            
        }

        private void SaveData()
        {
            string phone_number_data = phone_number.Text;
            string server_data = server.Text;
            string password_data = password.Text;
            string display_data = display_name.Text;
            string api = api_data.Text;
            string id = id_data.Text;

            if (IsValid(phone_number_data, "phone") && IsValid(server_data, "server"))
            {
                connStatus.Text = "     Spremenjeno!";
                Services.Services.AddUpdateAppSettings("SIPUsername", display_data);
                Services.Services.AddUpdateAppSettings("SIPServer", server_data);
                Services.Services.AddUpdateAppSettings("SIPPassword", password_data);
                Services.Services.AddUpdateAppSettings("SIPPhoneNumber", phone_number_data);
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

        private async void test_data_Click(object sender, RoutedEventArgs e)
        {
            // testing
            this.Visibility = Visibility.Visible;
            isSuccess = false;
            string phone_number_data = phone_number.Text;
            string server_data = server.Text;
            string password_data = password.Text;
            string display_data = display_name.Text;
            _sipRegistrationClient.Start();
        }


        private async Task Initialize()
        {
            await _sipTransportManager.InitialiseSIP();
            string username_data = phone_number.Text;
            string server_data = server.Text;
            string password_data = password.Text;
            string listeningEndPoints = null;
            foreach (var sipChannel in _sipTransportManager.SIPTransport.GetSIPChannels())
            {
                SIPEndPoint sipChannelEP = sipChannel.ListeningSIPEndPoint.CopyOf();
                sipChannelEP.ChannelID = null;
                listeningEndPoints += (listeningEndPoints == null) ? sipChannelEP.ToString() : $", {sipChannelEP}";
            }
            string port = $"Listening on: {listeningEndPoints}";
            _sipRegistrationClient = new SIPRegistrationUserAgent(
                _sipTransportManager.SIPTransport,
                m_sipUsername,
                m_sipPassword,
                m_sipServer,
                REGISTRATION_EXPIRY);
       
            _sipRegistrationClient.RegistrationSuccessful += _sipRegistrationClient_RegistrationSuccessful;
            _sipRegistrationClient.RegistrationFailed += _sipRegistrationClient_RegistrationFailed;

        }
        private void _sipRegistrationClient_RegistrationFailed(SIPURI arg1, string arg2)
        {
            this.Dispatcher.Invoke(() =>
            {
                connStatus.Text = "     Neuspešna prijava.";
            });

            _sipRegistrationClient.Stop();

            MessageBox.Show($"There was an error. {arg2}");
        }

        private void _sipRegistrationClient_RegistrationSuccessful(SIPURI obj)
        {
            this.Dispatcher.Invoke(() =>
            {
                connStatus.Text = "     Uspešna prijava.";
            });

            _sipRegistrationClient.Stop();

            

        }
        /// <summary>
        /// Enable detailed SIP log messages.
        /// </summary>
        private static void EnableTraceLogs(SIPTransport sipTransport)
        {
            sipTransport.SIPRequestInTraceEvent += (localEP, remoteEP, req) =>
            {
                Console.WriteLine($"Request received: {localEP}<-{remoteEP}");
                Console.WriteLine(req.ToString());
            };

            sipTransport.SIPRequestOutTraceEvent += (localEP, remoteEP, req) =>
            {
                Console.WriteLine($"Request sent: {localEP}->{remoteEP}");
                Console.WriteLine(req.ToString());
            };

            sipTransport.SIPResponseInTraceEvent += (localEP, remoteEP, resp) =>
            {
                Console.WriteLine($"Response received: {localEP}<-{remoteEP}");
                Console.WriteLine(resp.ToString());
            };

            sipTransport.SIPResponseOutTraceEvent += (localEP, remoteEP, resp) =>
            {
                Console.WriteLine($"Response sent: {localEP}->{remoteEP}");
                Console.WriteLine(resp.ToString());
            };

            sipTransport.SIPRequestRetransmitTraceEvent += (tx, req, count) =>
            {
                Console.WriteLine($"Request retransmit {count} for request {req.StatusLine}, initial transmit {DateTime.Now.Subtract(tx.InitialTransmit).TotalSeconds.ToString("0.###")}s ago.");
            };

            sipTransport.SIPResponseRetransmitTraceEvent += (tx, resp, count) =>
            {
                Console.WriteLine($"Response retransmit {count} for response {resp.ShortDescription}, initial transmit {DateTime.Now.Subtract(tx.InitialTransmit).TotalSeconds.ToString("0.###")}s ago.");
            };
        }

        private void MwiSubscriber_SubscriptionFailed(SIPURI arg1, SIPResponseStatusCodesEnum arg2, string arg3)
        {
            this.Dispatcher.Invoke(() =>
            {
                connStatus.Text = "     Neuspešna prijava.";
            });
            sipTransport.Shutdown();
            mwiSubscriber.Stop();
        }

        private void MwiSubscriber_SubscriptionSuccessful(SIPURI obj)
        {
            this.Dispatcher.Invoke(() =>
            {
                connStatus.Text = "     Uspešna prijava.";
            });
            sipTransport.Shutdown();
            mwiSubscriber.Stop();
        }
    }
}