using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NodeLabFarm.Models
{
    public class ScheduleModel : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _scriptName = string.Empty;
        private string _scriptPath = string.Empty;
        private ObservableCollection<string> _deviceSerials = new();
        private double _delayBetweenDevices = 5.0; // Seconds
        private DateTime _startTime = DateTime.Now;
        private int _repeatCount = 1;
        private string _status = "Pending";
        private string _name = "Lịch chạy mới";

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string ScriptName
        {
            get => _scriptName;
            set { _scriptName = value; OnPropertyChanged(); }
        }

        public string ScriptPath
        {
            get => _scriptPath;
            set { _scriptPath = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> DeviceSerials
        {
            get => _deviceSerials;
            set { _deviceSerials = value; OnPropertyChanged(); }
        }

        public double DelayBetweenDevices
        {
            get => _delayBetweenDevices;
            set { _delayBetweenDevices = value; OnPropertyChanged(); }
        }

        public DateTime StartTime
        {
            get => _startTime;
            set { _startTime = value; OnPropertyChanged(); }
        }

        public int RepeatCount
        {
            get => _repeatCount;
            set { _repeatCount = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
