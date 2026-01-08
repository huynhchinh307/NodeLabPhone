using AdvancedSharpAdbClient.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NodeLabFarm.Models
{
    public class AdbDeviceModel : INotifyPropertyChanged
    {
        private DeviceData _deviceData;
        private string _customName = string.Empty;
        private string _status = "Disconnected";
        private string _ipAddress = "127.0.0.1";
        private bool _isRenaming = false;
        private bool _isSelected = false;
        private int _batteryLevel = 100;
        private bool _isCharging = false;
        private int _scriptCount = 0;
        private string _phoneNumber = "+1 (555) 000-0000";
        private int _index;
        private string _scriptStatus = "Sẵn sàng";
        private ScriptModel? _selectedScript;
        private bool _isScriptRunning = false;
        private bool _isScriptPaused = false;
        private System.Windows.Media.ImageSource? _deviceScreen;

        public AdbDeviceModel(DeviceData deviceData)
        {
            _deviceData = deviceData;
            // Use fixed image for all devices
            ImageSource = "/Assets/devices.png";
        }

        public string ImageSource { get; set; }

        public System.Windows.Media.ImageSource? DeviceScreen
        {
            get => _deviceScreen;
            set { _deviceScreen = value; OnPropertyChanged(); }
        }
        
        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                OnPropertyChanged();
            }
        }

        public string Serial => _deviceData.Serial;
        public string Model => _deviceData.Model;
        
        public string CustomName
        {
            get => _customName;
            set
            {
                _customName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        public string Name => string.IsNullOrEmpty(CustomName) ? (_deviceData.Model ?? "Unknown Device") : CustomName;
        
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public int BatteryLevel
        {
            get => _batteryLevel;
            set { _batteryLevel = value; OnPropertyChanged(); }
        }

        public bool IsCharging
        {
            get => _isCharging;
            set { _isCharging = value; OnPropertyChanged(); }
        }

        public int ScriptCount
        {
            get => _scriptCount;
            set { _scriptCount = value; OnPropertyChanged(); }
        }

        public string PhoneNumber
        {
            get => _phoneNumber;
            set { _phoneNumber = value; OnPropertyChanged(); }
        }

        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                _ipAddress = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public bool IsRenaming
        {
            get => _isRenaming;
            set
            {
                _isRenaming = value;
                OnPropertyChanged();
            }
        }

        public string ScriptStatus
        {
            get => _scriptStatus;
            set
            {
                _scriptStatus = value;
                OnPropertyChanged();
            }
        }

        public ScriptModel? SelectedScript
        {
            get => _selectedScript;
            set
            {
                _selectedScript = value;
                OnPropertyChanged();
            }
        }

        public bool IsScriptRunning
        {
            get => _isScriptRunning;
            set { _isScriptRunning = value; OnPropertyChanged(); }
        }

        public bool IsScriptPaused
        {
            get => _isScriptPaused;
            set { _isScriptPaused = value; OnPropertyChanged(); }
        }

        public DeviceState State => _deviceData.State;

        public string DisplayName => $"{Name} ({Serial}) - {State}";

        public DeviceData GetDeviceData() => _deviceData;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
