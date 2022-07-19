using ShinyCall.Sqlite;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace ShinyCall.MVVM.View
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        private bool isSuccess;

        public SettingsView()
        {
            InitializeComponent();
            InitializeView();
        }

        private void InitializeView()
        {

            password.Text = Services.Services.GetAppSettings("SIPPassword");
            server.Text = Services.Services.GetAppSettings("SIPServer");
            display_name.Text = Services.Services.GetAppSettings("SIPUsername");
            phone_number.Text = Services.Services.GetAppSettings("SIPPhoneNumber");
            api_data.Text = Services.Services.GetAppSettings("APIaddress");
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

        private void test_data_Click(object sender, RoutedEventArgs e)
        {
            // testing
            this.Visibility = Visibility.Visible;
            isSuccess = false;
            string phone_number_data = phone_number.Text;
            string server_data = server.Text;
            string password_data = password.Text;
            string display_data = display_name.Text;
            sipTransport = new SIPTransport();
            sipTransport.EnableTraceLogs();
            var mwiURI = SIPURI.ParseSIPURIRelaxed($"{phone_number.Text}@{server_data}");
            int expiry = 5;          
            mwiSubscriber = new SIPNotifierClient(sipTransport, null, SIPEventPackagesEnum.MessageSummary, mwiURI, phone_number_data, null, password_data, expiry, null);
            mwiSubscriber.Start();
            this.Dispatcher.Invoke(() =>
            {
                connStatus.Text = "     Povezovanje...";
            });          
            mwiSubscriber.SubscriptionSuccessful += MwiSubscriber_SubscriptionSuccessful; ;
            mwiSubscriber.SubscriptionFailed += MwiSubscriber_SubscriptionFailed;

         
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
