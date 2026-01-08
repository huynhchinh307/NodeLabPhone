using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NodeLabFarm.Models
{
    public class DashboardTaskModel : INotifyPropertyChanged
    {
        private string _scriptName = string.Empty;
        private string _status = "Pending";
        private DateTime _startTime;

        public string ScriptName
        {
            get => _scriptName;
            set { _scriptName = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public DateTime StartTime
        {
            get => _startTime;
            set { _startTime = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
