using AsterNET.Manager;
using AsterNET.Manager.Event;
using ShinyCall;
using ShinyCall.Mappings;
using ShinyCall.MVVM.ViewModel;
using ShinyCall.Services;
using ShinyCall.Sqlite;
using SIPSorcery.SIP.App;
using SIPSorcery.SoftPhone;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using ToastNotifications;
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
            InitializeComponent();
            BusinessLogic();
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


        Notifier notifier_reload = new Notifier(cfg =>
        {
            cfg.PositionProvider = new WindowPositionProvider(
                parentWindow: Application.Current.MainWindow,
                corner: Corner.TopRight,
                offsetX: 10,
                offsetY: 10);

            cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                notificationLifetime: TimeSpan.FromSeconds(5),
                maximumNotificationCount: MaximumNotificationCount.FromCount(1));
            cfg.DisplayOptions.Width = 200;

            cfg.Dispatcher = Application.Current.Dispatcher;
        });


        private bool alreadyShown = false;

        private async void BusinessLogic()
        {
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
             
                string state = e.State;
                string callerID = e.CallerId;
                if ((state == "Ringing") | (e.ChannelState == "5"))
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
                                notifier_reload.ShowInformation($"Dohodni klic od {calleridnumber}-{calleridname}.");
                                Application.Current.MainWindow.Topmost = true;
                                Application.Current.MainWindow.WindowState = WindowState.Normal;
                                notifier_reload.ShowInformation($"Dohodni klic od {calleridnumber}-{calleridname}.");
                            
                    
                                // Ringing
                                ContactsModel? contact = new ContactsModel();
                                try
                                {
                                    ContactsModel contact_number = new ContactsModel();
                                    contact_number.phone = Int32.Parse(calleridnumber);
                                    contact = SqliteDataAccess.GetContact(contact_number);

                                }
                                catch (Exception)
                                {
                                }

                                if (contact.name != null)
                                {
                                    nameCaller = $"Dohodni klic od {contact.name + " " + contact.phone}.";
                                    calleridname = contact.name;
                                    calleridnumber = contact.phone.ToString();
                                }
                                else
                                {
                                    nameCaller = $"Dohodni klic od {calleridnumber}-{calleridname}";
                                }
                                this.Dispatcher.Invoke(() =>
                                {
                                    notifier.ShowInformation(nameCaller);
                                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Sound\phone.wav");
                                    Console.Beep(1000, 5000);
                                    SoundPlayer player = new SoundPlayer(path);
                                    player.Load();
                                    player.Play();
                                });
                                try
                                {
                                    if (!alreadyShown)
                                    {
                                        Application.Current.Dispatcher.Invoke((Action)delegate
                                        {
                                            APIHelper.InitializeClient();
                                            string id = ConfigurationManager.AppSettings["UserData"];
                                            string phone = ConfigurationManager.AppSettings["SIPPhoneNumber"];
                                            Random random = new Random();
                                            var popupt = Task.Run(async () => await APIAccess.GetPageAsync(id_unique.ToString(), calleridnumber, id, phone)).Result;
                                            Popup popup = new Popup((int)popupt.Data.Attributes.PopupDuration, popupt.Data.Attributes.Url.ToString(), (int)popupt.Data.Attributes.PopupHeight, (int)popupt.Data.Attributes.PopupWidth);
                                            popup.Show();
                                            alreadyShown = true;
                                            notifier.ShowInformation(nameCaller);

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
                    } else
                    {
                        notifier.ShowInformation(nameCaller);
                    }
                }
                else if ((state == "Ring") | (e.ChannelState == "4"))
                {
                    calleridname = e.CallerIdName;
                    calleridnumber = e.CallerIdNum;
                    caller_model = new CallModel();
                    caller_model.caller = calleridnumber;
                    id_unique = Guid.NewGuid();
                    MainBoleanValue = false;
                }
                else if ((state == "Up") | (e.ChannelState == "6") && MainBoleanValue&&commited_guid!=id_unique)
                {
                    caller_model.status = "Answered";
                    caller_model.time = DateTime.Now.ToString();
                    caller_model.caller = $"{calleridnumber}-{calleridname}";
                    SqliteDataAccess.InsertCallHistory(caller_model);
                    commited_guid = id_unique;
                    answered = true;
                    MainBoleanValue = false;
                }
            }
        }

        private void Manager_Hangup(object sender, HangupEvent e)
        {
            try
            {
                if (commited_guid != id_unique && MainBoleanValue)
                {
                    caller_model.status = "Missed";
                    caller_model.time = DateTime.Now.ToString();
                    caller_model.caller = $"{calleridnumber}-{calleridname}";
                    SqliteDataAccess.InsertCallHistory(caller_model);
                    commited_guid = id_unique;
                    alreadyShown = false;
                    notifier.Dispose();
                    notifier_reload.Dispose();
                    MainBoleanValue = false;
                    
                }
            } catch { }
        }      
    }
}