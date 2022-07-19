using ShinyCall.Mappings;
using ShinyCall.Sqlite;
using SIPSorcery.SoftPhone;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
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

namespace ShinyCall.MVVM.View
{
    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
            InitializeView();           
            var task = Task.Run(async () => await RunForever());
        }

        public async Task RunForever()
        {
            await Task.Run(() =>
            {
                while(true)
                {
                    UpdateUI(); 
                    Thread.Sleep(2000);
                }
            });
        }
        private void UpdateUI()
        {

            this.Dispatcher.Invoke(new Action(() =>
            {

                wNumber.Text = $"Telefonska številka je: {ConfigurationManager.AppSettings["SIPPhoneNumber"]}";
                wServer.Text = $"Server je: {ConfigurationManager.AppSettings["SIPServer"]}";
                var history = SqliteDataAccess.LoadCalls();
                history.Reverse();
                list_view.ItemsSource = GetListFromModels(history);

                var last_calls = history.Skip(Math.Max(0, history.Count() - 3));
                if (last_calls.Count() > 0)
                {
                    try
                    {
                        CallModel? first = (CallModel)history.ElementAt(0);
                        first_call.Text = ReturnStringOrDefault(first);

                    }
                    catch { }
                    try
                    {
                        CallModel? second = (CallModel)history.ElementAt(1);
                        second_call.Text = ReturnStringOrDefault(second);

                    }
                    catch { }

                    try
                    {
                        CallModel? third = (CallModel)history.ElementAt(2);
                        third_call.Text = ReturnStringOrDefault(third);

                    }
                    catch { }



                }
            }));
          
            
        }

        private System.Collections.IEnumerable GetListFromModels(List<CallModel> history)
        {
            foreach(var call in history)
            {
                yield return $"{call.caller}-{call.status}-{call.time}";
            }
        }

        private void InitializeView()
        {
            UpdateUI();
        }

        private string ReturnStringOrDefaultList(CallModel? value)
        {
            if (value != null)
            {
                return $"{value.caller}-{value.status}";
            }
            return String.Empty;
        }

        private string ReturnStringOrDefault(CallModel? value)
        {
        
            if(value!=null)
            {
                DateTime? date = DateTime.Parse(value.time);
                String content = $"{value.caller}\n{value.status}\n{value.time}\n{date.Value.Date}";
                return content;
            } 
            return String.Empty;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SqliteDataAccess.DeleteHistory();
            first_call.Text = String.Empty;
            second_call.Text = String.Empty;
            third_call.Text = String.Empty; 
        }
    }
}
