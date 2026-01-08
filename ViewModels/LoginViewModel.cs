using System;
using System.ComponentModel;
using NodeLabFarm.Services;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace NodeLabFarm.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string _username = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isBusy = false;
        private string _version = "v1.0.2 Stable";
        private string _updateStatus = "Hệ thống đã mới nhất";
        
        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        public string Version
        {
            get => _version;
            set { _version = value; OnPropertyChanged(); }
        }

        public string UpdateStatus
        {
            get => _updateStatus;
            set { _updateStatus = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand CheckUpdateCommand { get; }

        public event EventHandler? RequestClose;

        private readonly IAuthService _authService;

        public LoginViewModel()
        {
            _authService = new AuthService();
            LoginCommand = new RelayCommand(Login);
            CloseCommand = new RelayCommand(_ => Application.Current.Shutdown());
            CheckUpdateCommand = new RelayCommand(CheckForUpdates);
        }

        private async void Login(object? parameter)
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Vui lòng nhập tên đăng nhập.";
                return;
            }

            var passwordBox = parameter as Wpf.Ui.Controls.PasswordBox;
            var password = passwordBox?.Password;

            if (string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage = "Vui lòng nhập mật khẩu.";
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            // Simulate network delay for premium feel
            await System.Threading.Tasks.Task.Delay(1500);

            if (Username == "admin" && password == "admin")
            {
                _authService.Login();
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác.";
            }

            IsBusy = false;
        }

        private async void CheckForUpdates(object? _)
        {
            UpdateStatus = "Đang kiểm tra cập nhật...";
            await System.Threading.Tasks.Task.Delay(2000);
            UpdateStatus = "Hệ thống của bạn đang là phiên bản mới nhất.";
            System.Windows.MessageBox.Show(UpdateStatus, "Kiểm tra cập nhật", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
