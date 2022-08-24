using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ShinyCall
{
    /// <summary>
    /// Interaction logic for Popup.xaml
    /// </summary>
    public partial class Popup : Window
    {
        // Prep stuff needed to remove close button on window.
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
    

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        public  Popup(int timeout, string url, int height, int width)
        {
            InitializeComponent();      
            Loaded += ToolWindow_Loaded;
            if (url == "")
            {
                CancelForm();
                return;
            }
            else if (timeout == 0)
            {
                Uri uri = new Uri(url);
                bSearch.Source = uri;
                this.Height = height;
                this.Width = width;
            }
            else
            {
                Task.Delay(new TimeSpan(0, 0, timeout)).ContinueWith(o => { CancelForm(); });
                Uri uri = new Uri(url);
                bSearch.Source = uri;
                this.Height = height;
                this.Width = width;
            }
        }

        private void CancelForm()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.Hide();
                this.Close();
            });
    
        }



        void ToolWindow_Loaded(object sender, RoutedEventArgs e)
        {



            // Code to put the windows to the bottom right.
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width;
            this.Top = desktopWorkingArea.Bottom - this.Height;
        }
    }
}
