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
        public  Popup(int timeot, string url, int height, int width)
        {
        
            InitializeComponent();
            bSearch.Navigating += BSearch_Navigating;
            Loaded += ToolWindow_Loaded;
            Task.Delay(new TimeSpan(0, 0, timeot)).ContinueWith(o => { CancelForm(); });
            bSearch.Navigate(url);
            this.Height = height;
            this.Width = width;
        }

        private void BSearch_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            dynamic activeX = this.bSearch.GetType().InvokeMember("ActiveXInstance",
           BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
           null, this.bSearch, new object[] { });

            activeX.Silent = true;
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
            // Code to remove close box from window
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);


            // Code to put the windows to the bottom right.
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width;
            this.Top = desktopWorkingArea.Bottom - this.Height;
        }
    }
}
