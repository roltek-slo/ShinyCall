using Microsoft.Toolkit.Mvvm.ComponentModel;
using ShinyCall.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SystemTrayApp.WPF;

using Microsoft.Toolkit.Mvvm.Input;
using ShinyCall;
namespace ShinyCall.MVVM.ViewModel
{
    internal class MainViewModel: ObservableRecipient
    {

        private NotifyIconWrapper.NotifyRequestRecord? _notifyRequest;
        private bool _showInTaskbar;
        private WindowState _windowState;
        private Visibility _visibility;
        public Core.RelayCommand HomeViewCommand { get; set; }
        public Core.RelayCommand SettingsCommand { get; set; }
        public Core.RelayCommand LastProjectCommand { get; set; }
        public HomeViewModel HomeVm { get; set; }
        public object _currentView { get; set; }
        public SettingsViewModel SettingsVM { get; set; }    
        public LastProjectViewModel LastProjectVM { get; set; } 



        public object CurrentView
        {
            get { return _currentView; }
            set { _currentView = value;
                OnPropertyChanged();
            }
        }


        public MainViewModel()
        {
            HomeVm = new HomeViewModel();
            LastProjectVM = new LastProjectViewModel(); 
            SettingsVM = new SettingsViewModel();
            CurrentView = HomeVm;


            HomeViewCommand = new Core.RelayCommand(o =>
            {
                CurrentView = HomeVm;
            });


            LastProjectCommand = new Core.RelayCommand(o =>
            {
                CurrentView = LastProjectVM;
            });

            SettingsCommand = new Core.RelayCommand(o =>
            {
                CurrentView = SettingsVM;
            });

            LoadedCommand = new Microsoft.Toolkit.Mvvm.Input.RelayCommand(Loaded);
            ClosingCommand = new RelayCommand<CancelEventArgs>(Closing);
            NotifyCommand = new Microsoft.Toolkit.Mvvm.Input.RelayCommand(() => Notify("Welcome to VOIP service!"));
            NotifyIconOpenCommand = new Microsoft.Toolkit.Mvvm.Input.RelayCommand(() => { WindowState = WindowState.Normal; });
            NotifyIconExitCommand = new Microsoft.Toolkit.Mvvm.Input.RelayCommand(() => { Application.Current.Shutdown(); });

        }




        public ICommand LoadedCommand { get; }
        public ICommand ClosingCommand { get; }
        public ICommand NotifyCommand { get; }
        public ICommand NotifyIconOpenCommand { get; }
        public ICommand NotifyIconExitCommand { get; }

        public WindowState WindowState
        {
            get => _windowState;
            set
            {
     
                ShowInTaskbar = true;
                SetProperty(ref _windowState, value);
                ShowInTaskbar = value != WindowState.Minimized;
            }
        }
       
        public bool ShowInTaskbar
        {
            get => _showInTaskbar;
            set => SetProperty(ref _showInTaskbar, value);
        }

        public NotifyIconWrapper.NotifyRequestRecord? NotifyRequest
        {
            get => _notifyRequest;
            set => SetProperty(ref _notifyRequest, value);
        }

        public void Notify(string message)
        {
            NotifyRequest = new NotifyIconWrapper.NotifyRequestRecord
            {
                Title = "Notify",
                Text = message,
                Duration = 1000
            };
            var debug = true;

        }

        private void showInterface()
        {

        }


        private void Loaded()
        {

        }

        private void Closing(CancelEventArgs? e)
        {
            if (e == null)
                return;
            e.Cancel = true;
            WindowState = WindowState.Minimized;
        }
    }
}
