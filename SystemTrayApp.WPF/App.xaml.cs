using AsterNET.Manager;
using AsterNET.Manager.Event;
using ShinyCall.Mappings;
using ShinyCall.MVVM.ViewModel;
using ShinyCall.Sqlite;
using SIPSorcery.SIP.App;
using SIPSorcery.SoftPhone;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SystemTrayApp.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private NotifyIconWrapper.NotifyRequestRecord? _notifyRequest;
        private MainViewModel context = new MainViewModel();
        private const int SIP_CLIENT_COUNT = 2;                             // The number of SIP clients (simultaneous calls) that the UI can handle.
        private const int ZINDEX_TOP = 10;
        private const int REGISTRATION_EXPIRY = 180;
        private string caller = string.Empty;
        private bool isMissedCall = true;
        private string m_sipUsername = SIPSoftPhoneState.SIPUsername;
        private string m_sipPassword = SIPSoftPhoneState.SIPPassword;
        private string m_sipServer = SIPSoftPhoneState.SIPServer;
        private bool m_useAudioScope = SIPSoftPhoneState.UseAudioScope;

        private SIPTransportManager _sipTransportManager;
        private List<SIPClient> _sipClients;
        private SoftphoneSTUNClient _stunClient;                    // STUN client to periodically check the public IP address.
        private SIPRegistrationUserAgent _sipRegistrationClient;    // Can be used to register with an external SIP provider if incoming calls are required.

#pragma warning disable CS0649
        private WriteableBitmap _client0WriteableBitmap;
        private WriteableBitmap _client1WriteableBitmap;
        private string? phone;
        private ManagerConnection manager;
        private CallModel caller_model;
        private Guid id_unique = Guid.NewGuid();
        private Guid commited_guid = Guid.NewGuid();

        public bool MainBoleanValue { get; private set; }
#pragma warning restore CS0649
        //private AudioScope.AudioScope _audioScope0;
        //private AudioScope.AudioScopeOpenGL _audioScopeGL0;
        //private AudioScope.AudioScope _audioScope1;
        //private AudioScope.AudioScopeOpenGL _audioScopeGL1;
        //private AudioScope.AudioScope _onHoldAudioScope;
        //private AudioScope.AudioScopeOpenGL _onHoldAudioScopeGL;

        public App()
        {
            InitializeComponent();
            BusinessLogic();
        }

        private async void BusinessLogic()
        {
            string SIPUsername = ConfigurationManager.AppSettings["SIPUsername"];
            string SIPPassword = ConfigurationManager.AppSettings["SIPPassword"];
            string SIPServer = ConfigurationManager.AppSettings["SIPServer"];
            string port = ConfigurationManager.AppSettings["SIPport"];
            phone = ConfigurationManager.AppSettings["SIPPhoneNumber"];
            manager = new ManagerConnection(SIPServer, Int32.Parse(port), SIPUsername, SIPPassword);
            manager.UnhandledEvent += new ManagerEventHandler(manager_Events);
            manager.NewState += new NewStateEventHandler(Monitoring_NewState);
            try
            {
                manager.Login();
                if (manager.IsConnected())
                {
                    Console.WriteLine("user name  : " + manager.Username);
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error connect\n" + ex.Message);
                manager.Logoff();
                Console.ReadLine();
            }
            void manager_Events(object sender, ManagerEvent e)
            {
                Console.WriteLine("Event : " + e.GetType().Name);
            }

            void Monitoring_NewState(object sender, NewStateEvent e)
            {
                string state = e.State;
                string callerID = e.CallerId;
                if ((state == "Ringing") | (e.ChannelState == "5"))
                {
                    string calleridname = e.CallerIdName;
                    string calleridnumber = e.CallerIdNum;
                    string channelstatedesc = e.ChannelStateDesc;
                    var datereceived = e.DateReceived;
                    if (!MainBoleanValue)
                    {
                        if (phone != String.Empty && phone == calleridnumber)
                        {
                            MainBoleanValue = true;
                        }
                        else
                        {
                            MainBoleanValue = false;
                        }
                    }
                }
                else if ((state == "Ring") | (e.ChannelState == "4"))
                {
                    string calleridname = e.CallerIdName;
                    string calleridnumber = e.CallerIdNum;
                    caller_model = new CallModel();
                    caller_model.caller = calleridnumber;
                    caller_model.time = e.DateReceived.ToLocalTime().ToString();
                    id_unique = Guid.NewGuid();
                }
                else if ((state == "Up") | (e.ChannelState == "6") && MainBoleanValue&&commited_guid!=id_unique)
                {
                    caller_model.status = "Answered";
                    caller_model.time = DateTime.Now.ToString();
                    SqliteDataAccess.InsertCallHistory(caller_model);
                    commited_guid = id_unique;
                    
                }
            }
        }
    }
}