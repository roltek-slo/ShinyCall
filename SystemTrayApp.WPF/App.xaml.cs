using AsterNET.Manager;
using AsterNET.Manager.Event;
using IWshRuntimeLibrary;
using Microsoft.Win32;
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
using System.Windows;
using ToastNotifications;
using ToastNotifications.Core;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System.Threading.Tasks;

namespace SystemTrayApp.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private MainViewModel context = new MainViewModel();
        private const int SIP_CLIENT_COUNT = 2;                             // The number of SIP clients (simultaneous calls) that the UI can handle.
        private const int ZINDEX_TOP = 10;
        private const int REGISTRATION_EXPIRY = 180;
        private string caller = string.Empty;

        private string m_sipUsername = SIPSoftPhoneState.SIPUsername;
        private string m_sipPassword = SIPSoftPhoneState.SIPPassword;
        private string m_sipServer = SIPSoftPhoneState.SIPServer;
        private bool m_useAudioScope = SIPSoftPhoneState.UseAudioScope;
#pragma warning disable CS0649
        private string? phone;
        private ManagerConnection manager;
        private CallModel caller_model = new CallModel();
        private Guid id_unique = Guid.NewGuid();
        private Guid commited_guid = Guid.NewGuid();
        public bool MainBoleanValue { get; private set; }
#pragma warning restore CS0649


        public App()
        {
            Crashes.SetEnabledAsync(true);
            Microsoft.AppCenter.AppCenter.Start("557b220c-9c91-4bc3-909f-90eefae8a75a", typeof(Analytics), typeof(Crashes));
            Crashes.NotifyUserConfirmation(UserConfirmation.AlwaysSend); /* Always send crash reports */ /*https://appcenter.ms/apps */
            Analytics.SetEnabledAsync(true);
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string path = System.IO.Path.Combine(startupPath, "ShinyCall.exe");
            CreateShortcut(Environment.ProcessPath);
            InitializeComponent();
            BusinessLogic();
        }



        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Crashes.TrackError(e.Exception);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Crashes.TrackError((Exception)e.ExceptionObject);
            var isTerminating = e.IsTerminating;
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Crashes.TrackError(e.Exception);
            e.Handled = true;
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
        private Popup popup;
        private Dictionary<string, string> attributes;
        private string receiver;
        private string callerNumber;
        private CallInformation callInformation;

        private async void BusinessLogic()
        {
            var options = new MessageOptions
            {
                ShowCloseButton = false, // set the option to show or hide notification close button
            };
            string reload = Services.GetAppSettings("reload");
            phone = ConfigurationManager.AppSettings["SIPPhoneNumber"];
            string password = Services.GetAppSettings("SIPPassword");
            string server = Services.GetAppSettings("SIPServer");
            string username = Services.GetAppSettings("SIPUsername");
            string port = Services.GetAppSettings("SIPport");
            manager = new ManagerConnection(server, Int32.Parse(port), username, password);
            manager.UnhandledEvent += new ManagerEventHandler(manager_Events);
            manager.NewState += new NewStateEventHandler(Monitoring_NewState);
            manager.Transfer += Manager_Transfer;
            manager.Dial += Manager_Dial;
            manager.NewChannel += Manager_NewChannel;

         
            try
            {
                manager.Login();
                if (manager.IsConnected())
                {
                    Analytics.TrackEvent($"Login {manager.Username}, time: {DateTime.Now}");
                }
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
                Analytics.TrackEvent("Error connect\n" + ex.Message);
                manager.Logoff();
            }
            void manager_Events(object sender, ManagerEvent e)
            {
                Analytics.TrackEvent("Event : " + e.GetType().Name);
            }

            void Monitoring_NewState(object sender, NewStateEvent e)
            {             
                if (e.Channel.Contains("SIP") && e.Channel.Contains(phone)&&e.ChannelStateDesc == "Up" && callerChannel.state == "Transfer")
                {
                    callerChannel.answered = true;
                } else if (callerChannel.selfChannel!=null && e.Channel.Contains(phone) && e.ChannelStateDesc == "Up")
                {
                    callerChannel.answered = true;
                }

            }
        }

   

   

        private void Manager_Dial(object sender, DialEvent e)
        {

            if (e.SubEvent == "Begin" && e.DialString == phone && e.Destination.Contains("SIP") && e.Destination.Contains(phone))
            {
                if(e.CallerIdNum!=string.Empty)
                {
                    callerChannel.number = e.CallerIdNum;
                 
                }
                if (e.CallerIdName != string.Empty)
                {
               
                    callerChannel.name = e.CallerIdName;
                }
                if (!callerChannel.shownAlready)
                {
                    Ringing();
                }
            }
            else if (callerChannel.number != string.Empty && e.Channel.Contains(callerChannel.number) && e.SubEvent == "End" && e.DialStatus == "NOANSWER")
            {

              
           
        



            }
            else if (callerChannel.number != string.Empty && e.Channel.Contains(phone) && e.SubEvent == "End" && e.DialStatus == "BUSY")
            {


        

                



            }
            else if (callerChannel.number != string.Empty && e.Channel.Contains(callerChannel.number) && e.SubEvent == "End" && e.DialStatus == "ANSWER" && callerChannel.state != "Transfer") {

                if (callerChannel.answered)
                {
                    EndCall();
                    callerChannel = new CallerChannel();

                }
                else
                {
           
               
                }

            }
            else if (callerChannel.number != string.Empty && e.Channel.Contains(callerChannel.number) && e.SubEvent == "End" && e.DialStatus == "CANCEL" && callerChannel.state != "Transfer")
            {

                if (callerChannel.answered)
                {
                    EndCall();
                    callerChannel = new CallerChannel();

                }
                else
                {
      
                  

                }

            }
            else if (callerChannel.number != string.Empty && e.Channel.Contains(callerChannel.number) && e.SubEvent == "End" && e.DialStatus == "ANSWER" && callerChannel.state == "Transfer")
            {
                if (callerChannel.answered)
                {
                    EndCall();
                    callerChannel = new CallerChannel();

                }
                else
                {
                   
         

                }
            }
                                  
            else if (callerChannel.number == string.Empty && e.CallerIdNum != null && e.CallerIdNum != string.Empty && e.Destination.Contains(phone))
            {
                callerChannel.number = e.CallerIdNum;
                callerChannel.name = e.CallerIdName;
            }
            else if (e.SubEvent == "End" && e.DialStatus == "ANSWER" && e.UniqueId == callerChannel.id)
            {
                EndCall();
                callerChannel = new CallerChannel();
            }
            else if (e.SubEvent == "End" && e.DialStatus == "CANCEL" && e.UniqueId == callerChannel.id)
            {
    
             
            }
        


        }

        private void EndCall()
        {
            caller_model.status = "Answered";
        
            SqliteDataAccess.UpdateCallHistory(caller_model);
        }

        List<NewChannelEvent> currentChannels = new List<NewChannelEvent>();
        private void Missed()
        {
            caller_model.status = "Missed";
            caller_model.time = DateTime.Now.ToString();
            callerChannel.time = DateTime.Now.ToString();
            caller_model.caller = $"{callerChannel.number}-{callerChannel.name}";
            SqliteDataAccess.InsertCallHistory(caller_model);
            callerChannel.shownAlready= true;

        }
        private void Manager_Transfer(object sender, TransferEvent e)
        {
            if (e.TransferExten == phone)
            {

                callerChannel.state = "Transfer";
                callerChannel.number = string.Empty;
                callerChannel.name = string.Empty;
                callerChannel.id = string.Empty;
                callerChannel.transferEvent = e;
                
            }
        }
        List<string> strings = new List<string>();
        private string i;
        public CallerChannel callerChannel = new CallerChannel();
        private void Manager_NewChannel(object sender, NewChannelEvent e)
        {

       
            if (e.Channel.Contains("SIP") && e.Attributes.Count >= 2 && e.Attributes["exten"].Contains(phone))
            {
                callerChannel.state = e.ChannelStateDesc;
                callerChannel.number = e.CallerIdNum;
                callerChannel.name = e.CallerIdName;
                callerChannel.id = e.UniqueId;
                callerChannel.shownAlready = false;
            } 
            if (e.Channel.Contains(phone) && e.Channel.Contains("SIP"))
            {
               
                callerChannel.selfChannel = e;
            }

         
          


          

        }
        public class CallerChannel
        {
            internal string time;

            public string name { get; set; } = string.Empty;
            public string number { get; set; } = string.Empty;
            public string id { get; set; } = string.Empty; 
            public string state { get; set; } = string.Empty;

            public NewChannelEvent callerChannel { get; set; }
            public bool answered { get; set; } = false;
            public NewChannelEvent selfChannel { get; set;  }
            public bool shownAlready { get; set; } = false;

            public TransferEvent transferEvent { get; set; }

        }
        private void Ringing()
        {
            this.Dispatcher.Invoke(() =>
            {
                try
                {
                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            APIHelper.InitializeClient();
                            string id = ConfigurationManager.AppSettings["UserData"];
                            string phone = ConfigurationManager.AppSettings["SIPPhoneNumber"];
                            var popupt = Task.Run(async () => await APIAccess.GetPageAsync(id_unique.ToString(), callerChannel.number, id, phone)).Result;
                            popup = new Popup((int)popupt.Data.Attributes.PopupDuration, popupt.Data.Attributes.Url.ToString(), (int)popupt.Data.Attributes.PopupHeight, (int)popupt.Data.Attributes.PopupWidth); popup.Show();
                            popup.Activate();
                            popup.Closed += Popup_Closed;
                            popup.Topmost = true;
                            callerChannel.shownAlready = true;

                        });                   
                }
                catch (Exception ex)
                {
                    Crashes.TrackError(ex);
                    Analytics.TrackEvent("Error line : " + 236.ToString());
                } finally
                {
                    Missed();
                }
            });
        }

        private void Popup_Closed(object? sender, EventArgs e)
        {
            popup = null;
        }
    }
        public class CallInformation
        {
            public string caller { get; set; }
            public string receiver { get; set; }
            public string callerName { get; set; }
            public string receiverName { get; set; }
            public string channelStateDescription { get; set; }
            public DateTime dateReceived { get; set; }
        }
}