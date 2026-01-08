using NodeLabFarm.Models;
using NodeLabFarm.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data;

namespace NodeLabFarm.ViewModels
{
    public class SchedulerViewModel : INotifyPropertyChanged
    {
        private readonly IAdbService _adbService;
        private readonly ObservableCollection<AdbDeviceModel> _allDevices;
        private readonly ObservableCollection<ScriptModel> _allScripts;
        private readonly ICollectionView _availableDevicesView;

        private string _newScheduleName = "Tác vụ tự động";
        private ScriptModel? _selectedScript;
        private double _delayBetweenDevices = 10;
        private DateTime _startDate = DateTime.Now;
        private int _startHour = DateTime.Now.Hour;
        private int _startMinute = DateTime.Now.Minute;
        private int _startSecond = 0;
        private int _repeatCount = 1;
        private bool _isTaskRunning = false;

        public ObservableCollection<ScheduleModel> Schedules { get; } = new();
        public ICollectionView AvailableDevices => _availableDevicesView;
        public ObservableCollection<ScriptModel> AvailableScripts => _allScripts;

        public SchedulerViewModel(ObservableCollection<AdbDeviceModel> devices, ObservableCollection<ScriptModel> scripts)
        {
            _adbService = new AdbService();
            _allDevices = devices;
            _allScripts = scripts;

            _availableDevicesView = new ListCollectionView(_allDevices);
            _availableDevicesView.Filter = d => (d as AdbDeviceModel)?.State == AdvancedSharpAdbClient.Models.DeviceState.Online;

            CreateScheduleCommand = new RelayCommand(CreateSchedule);
            DeleteScheduleCommand = new RelayCommand(DeleteSchedule);
            StartScheduleCommand = new RelayCommand(async s => await RunScheduleAsync(s as ScheduleModel));
        }

        public string NewScheduleName
        {
            get => _newScheduleName;
            set { _newScheduleName = value; OnPropertyChanged(); }
        }

        public ScriptModel? SelectedScript
        {
            get => _selectedScript;
            set { _selectedScript = value; OnPropertyChanged(); }
        }

        public double DelayBetweenDevices
        {
            get => _delayBetweenDevices;
            set { _delayBetweenDevices = value; OnPropertyChanged(); }
        }

        public DateTime StartDate
        {
            get => _startDate;
            set { _startDate = value; OnPropertyChanged(); }
        }

        public int StartHour
        {
            get => _startHour;
            set { _startHour = value; OnPropertyChanged(); }
        }

        public int StartMinute
        {
            get => _startMinute;
            set { _startMinute = value; OnPropertyChanged(); }
        }

        public int StartSecond
        {
            get => _startSecond;
            set { _startSecond = value; OnPropertyChanged(); }
        }

        public int RepeatCount
        {
            get => _repeatCount;
            set { _repeatCount = value; OnPropertyChanged(); }
        }

        public ICommand CreateScheduleCommand { get; }
        public ICommand DeleteScheduleCommand { get; }
        public ICommand StartScheduleCommand { get; }

        private void CreateSchedule(object? _)
        {
            if (SelectedScript == null)
            {
                MessageBox.Show("Vui lòng chọn kịch bản!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedDevices = AvailableDevices.Cast<AdbDeviceModel>().Where(d => d.IsSelected).Select(d => d.Serial).ToList();
            if (!selectedDevices.Any())
            {
                MessageBox.Show("Vui lòng chọn ít nhất một thiết bị!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var startTime = new DateTime(
                StartDate.Year, StartDate.Month, StartDate.Day,
                StartHour, StartMinute, StartSecond
            );

            var schedule = new ScheduleModel
            {
                Name = NewScheduleName,
                ScriptName = SelectedScript.Name,
                DeviceSerials = new ObservableCollection<string>(selectedDevices),
                DelayBetweenDevices = DelayBetweenDevices,
                StartTime = startTime,
                RepeatCount = RepeatCount,
                Status = "Ready"
            };

            Schedules.Add(schedule);
            
            // Reset form and clear selection
            NewScheduleName = "Tác vụ tự động";
            foreach (var device in _allDevices)
            {
                device.IsSelected = false;
            }
        }

        private void DeleteSchedule(object? parameter)
        {
            if (parameter is ScheduleModel schedule)
            {
                Schedules.Remove(schedule);
            }
        }

        private async Task RunScheduleAsync(ScheduleModel? schedule)
        {
            if (schedule == null) return;

            schedule.Status = "Running";
            
            try
            {
                // Placeholder for actual execution logic
                // In a real app, we'd spawn a background task here
                await Task.Run(async () => {
                    for (int r = 0; r < schedule.RepeatCount; r++)
                    {
                        foreach (var serial in schedule.DeviceSerials)
                        {
                            // Mocking execution
                            await Task.Delay(TimeSpan.FromSeconds(schedule.DelayBetweenDevices));
                        }
                    }
                });

                schedule.Status = "Completed";
            }
            catch (Exception)
            {
                schedule.Status = "Error";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
