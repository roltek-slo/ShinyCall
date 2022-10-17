using AsterNET.Manager;
using AsterNET.Manager.Event;
using IWshRuntimeLibrary;
using Mappings;
using Microsoft.Win32;
using ShinyCall;
using ShinyCall.Mappings;
using ShinyCall.MVVM.ViewModel;
using ShinyCall.Services;
using ShinyCall.Sqlite;
using SIPSorcery.Net;
using SIPSorcery.SIP.App;
using SIPSorcery.SoftPhone;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using ToastNotifications;
using ToastNotifications.Core;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

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
        private List<RedirectModel> rm = new List<RedirectModel>();
#pragma warning disable CS0649
        private WriteableBitmap _client0WriteableBitmap;
        private WriteableBitmap _client1WriteableBitmap;
        private string? phone;
        private ManagerConnection manager;
        private CallModel caller_model;
        private Guid id_unique = Guid.NewGuid();
        private Guid commited_guid = Guid.NewGuid();
        private bool answered = false;
        private string calleridname;
        private string calleridnumber;
        private string nameCaller;

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
            string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string path = System.IO.Path.Combine(startupPath, "ShinyCall.exe");

            CreateShortcut(Environment.ProcessPath);
            InitializeComponent();
            BusinessLogic();
        }



        public void CreateShortcut(string app)
        {
            string link = Environment.GetFolderPath(Environment.SpecialFolder.Startup)
                + Path.DirectorySeparatorChar + "ShinyCall" + ".lnk";
            var shell = new WshShell();
            var shortcut = shell.CreateShortcut(link) as IWshShortcut;
            shortcut.TargetPath = app;
            shortcut.WorkingDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            //shortcut...
            shortcut.Save();
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
                maximumNotificationCount: MaximumNotificationCount.FromCount(3));
            cfg.DisplayOptions.Width = 200;

            cfg.Dispatcher = Application.Current.Dispatcher;
        });





        private bool alreadyShown = false;
        private int answerC = 0;

        private async void BusinessLogic()
        {
            var options = new MessageOptions
            {
                ShowCloseButton = false, // set the option to show or hide notification close button
            };
            string reload = Services.GetAppSettings("reload");
            string SIPUsername = ConfigurationManager.AppSettings["SIPUsername"];
            string SIPPassword = ConfigurationManager.AppSettings["SIPPassword"];
            string SIPServer = ConfigurationManager.AppSettings["SIPServer"];
            string port = ConfigurationManager.AppSettings["SIPport"];
            phone = ConfigurationManager.AppSettings["SIPPhoneNumber"];
            manager = new ManagerConnection(SIPServer, Int32.Parse(port), SIPUsername, SIPPassword);
            manager.UnhandledEvent += new ManagerEventHandler(manager_Events);
            manager.NewState += new NewStateEventHandler(Monitoring_NewState);
            manager.Hangup += Manager_Hangup;

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


                string cState = e.ChannelState;
                string state = e.State;



                string callerID = e.CallerId;
                if (e.ChannelState == "5")
                {
                    string calleridname_inner = e.CallerIdName;
                    string calleridnumber_inner = e.CallerIdNum;
                    string channelstatedesc = e.ChannelStateDesc;
                    var datereceived = e.DateReceived;
                    if (!MainBoleanValue)
                    {
                        if (phone != String.Empty && phone == calleridnumber_inner)
                        {
                            MainBoleanValue = true;
                            this.Dispatcher.Invoke(() =>
                            {

                                try
                                {
                                    if (!alreadyShown)
                                    {
                                        Application.Current.Dispatcher.Invoke((Action)delegate
                                        {
                                            APIHelper.InitializeClient();
                                            string id = ConfigurationManager.AppSettings["UserData"];
                                            string phone = ConfigurationManager.AppSettings["SIPPhoneNumber"];
                                            var popupt = Task.Run(async () => await APIAccess.GetPageAsync(id_unique.ToString(), calleridnumber, id, phone)).Result;
                                            Popup popup = new Popup((int)popupt.Data.Attributes.PopupDuration, popupt.Data.Attributes.Url.ToString(), (int)popupt.Data.Attributes.PopupHeight, (int)popupt.Data.Attributes.PopupWidth);
                                            popup.Show();
                                            popup.Activate();
                                            popup.Topmost = true;
                                            alreadyShown = true;
                                            notifier.ShowInformation($"Dohodni klic od {calleridnumber}-{calleridname}.", options);
                                        });
                                    }
                                }
                                catch
                                {
                                }
                            });
                        }
                        else
                        {
                            MainBoleanValue = false;
                            return;
                        }
                    }
                    else
                    {
                        if (rm.Count >= 1)
                        {
                            notifier.ShowInformation($"Dohodni klic od {rm.ElementAt(0).number}-{rm.ElementAt(0).person}.", options);

                        }
                        else
                        {
                            notifier.ShowInformation($"Dohodni klic od {calleridnumber}-{calleridname}.", options);

                        }
                    }
                }
                else if (e.ChannelState == "4")
                {
                    calleridname = e.CallerIdName;
                    calleridnumber = e.CallerIdNum;
                    rm.Add(new RedirectModel { number = calleridnumber, person = calleridname });
                    caller_model = new CallModel();
                    caller_model.caller = calleridnumber;
                    id_unique = Guid.NewGuid();
                    MainBoleanValue = false;
                }
                else if ((e.ChannelState == "6" && MainBoleanValue && commited_guid != id_unique) || answerC>=2)
                {
                    caller_model.status = "Answered";
                    caller_model.time = DateTime.Now.ToString();
                    if (rm.Count >= 1)
                    {
                        caller_model.caller = $"{rm.ElementAt(0).number}-{rm.ElementAt(0).person}";
                    }
                    else
                    {
                        caller_model.caller = $"{calleridnumber}-{calleridname}";
                    }
                    SqliteDataAccess.InsertCallHistory(caller_model);
                    commited_guid = id_unique;
                    answered = true;
                    MainBoleanValue = false;
                    rm.Clear();
                } else if(e.ChannelState == "6")
                {
                    answerC += 1;
                }                
            }
        }

        private void Manager_Hangup(object sender, HangupEvent e)
        {

            var s = answerC;
            var ss = "stop";
            try
            {
                if (commited_guid != id_unique && MainBoleanValue)
                {
                    var test = rm;
                    caller_model.status = "Missed";
                    caller_model.time = DateTime.Now.ToString();
                    if (rm.Count >= 1)
                    {
                        caller_model.caller = $"{rm.ElementAt(0).number}-{rm.ElementAt(0).person}";

                    }
                    else
                    {
                        caller_model.caller = $"{calleridnumber}-{calleridname}";
                    }
                    SqliteDataAccess.InsertCallHistory(caller_model);
                    commited_guid = id_unique;
                    alreadyShown = false;
                    MainBoleanValue = false;
                    rm.Clear();
                }
            }
            catch { }
        }
    }
}