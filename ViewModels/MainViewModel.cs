using NodeLabFarm.Models;
using NodeLabFarm.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace NodeLabFarm.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IAdbService _adbService;
        private AdbDeviceModel? _selectedDevice;
        private string _commandInput = string.Empty;
        private string _commandOutput = string.Empty;
        private string _currentView = "Dashboard";
        private string _adbPath = "adb.exe";
        private ScriptModel? _currentScript;
        private SchedulerViewModel? _scheduler;
        private bool _isLoading = true;
        private string _loadingStatus = "Đang khởi tạo hệ thống...";
        private ObservableCollection<DashboardTaskModel> _runningDashboardTasks = new();
        private ObservableCollection<AdbDeviceModel> _allDevicesCollection = new();
        private int _runningThreadsCount = 0;


        public ICommand OpenDeviceCommand { get; }

        public MainViewModel()
        {
            _adbService = new AdbService();
            Devices = new ObservableCollection<AdbDeviceModel>();
            
            _adbPath = _adbService.GetAdbPath();

            RefreshCommand = new RelayCommand(async _ => await RefreshDevicesAsync());
            ExecuteCommand = new RelayCommand(async _ => await ExecuteCommandAsync(), _ => SelectedDevice != null && !string.IsNullOrWhiteSpace(CommandInput));
            NavigateCommand = new RelayCommand(view => CurrentView = (view as string) ?? "Devices");
            LogoutCommand = new RelayCommand(_ => Logout());
            RenameDeviceCommand = new RelayCommand(RenameDevice);
            BrowseAdbCommand = new RelayCommand(_ => BrowseAdb());
            ApplySettingsCommand = new RelayCommand(async _ => await ApplySettingsAsync());
            OpenDeviceCommand = new RelayCommand(async device => await OpenDeviceAsync(device as AdbDeviceModel));
            RunDashboardScriptCommand = new RelayCommand(RunDashboardScript);
            RunDeviceScriptCommand = new RelayCommand(RunDeviceScript);
            PauseDeviceScriptCommand = new RelayCommand(PauseDeviceScript);
            StopDeviceScriptCommand = new RelayCommand(StopDeviceScript);

            NextDevicePageCommand = new RelayCommand(_ => ChangeDevicePage(1), _ => DeviceCurrentPage < DeviceTotalPages);
            PreviousDevicePageCommand = new RelayCommand(_ => ChangeDevicePage(-1), _ => DeviceCurrentPage > 1);

            // Script Commands
            NextPageCommand = new RelayCommand(_ => ChangePage(1), _ => CurrentPage < TotalPages);
            PreviousPageCommand = new RelayCommand(_ => ChangePage(-1), _ => CurrentPage > 1);
            CreateScriptCommand = new RelayCommand(CreateNewScript);
            EditScriptCommand = new RelayCommand(EditScript);
            DeleteScriptCommand = new RelayCommand(DeleteScript);

            Scheduler = new SchedulerViewModel(Devices, AllScripts);

            // Start initialization
            _ = InitializeAdb();
            _ = StartLiveMonitoring();
        }

        private async Task StartLiveMonitoring()
        {
            while (true)
            {
                try
                {
                    if (CurrentView == "Dashboard")
                    {
                        var onlineDevices = AllDevices.Where(d => d.Status == "Active").ToList();
                        
                        // Parallel update for all devices to simulate "streaming"
                        var tasks = onlineDevices.Select(async device => 
                        {
                            var screenshot = await _adbService.GetScreenshotAsync(device.Serial);
                            if (screenshot != null)
                            {
                                device.DeviceScreen = screenshot;
                            }
                        });

                        await Task.WhenAll(tasks);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in live monitoring: {ex.Message}");
                }
                
                // 200ms delay = ~5 FPS, providing a smooth streaming-like feel
                await Task.Delay(200); 
            }
        }

        private ObservableCollection<AdbDeviceModel> _allDevices = new();
        private ObservableCollection<AdbDeviceModel> _devices = new();
        private string _deviceSearchText = string.Empty;
        private int _deviceCurrentPage = 1;
        private int _deviceItemsPerPage = 5;
        private int _deviceTotalPages = 1;
        private double _deviceRowHeight = 60;

        public int RunningThreadsCount
        {
            get => _runningThreadsCount;
            set { _runningThreadsCount = value; OnPropertyChanged(); }
        }

        private int _totalDevicesCount;
        private int _onlineDevicesCount;

        public int TotalDevicesCount
        {
            get => _totalDevicesCount;
            set { _totalDevicesCount = value; OnPropertyChanged(); }
        }

        public int OnlineDevicesCount
        {
            get => _onlineDevicesCount;
            set { _onlineDevicesCount = value; OnPropertyChanged(); }
        }

        public ObservableCollection<AdbDeviceModel> AllDevices => _allDevicesCollection;

        public ObservableCollection<AdbDeviceModel> Devices
        {
            get => _devices;
            set { _devices = value; OnPropertyChanged(); }
        }

        public string DeviceSearchText
        {
            get => _deviceSearchText;
            set
            {
                _deviceSearchText = value;
                OnPropertyChanged();
                DeviceCurrentPage = 1;
                UpdatePagedDevices();
            }
        }

        public int DeviceCurrentPage
        {
            get => _deviceCurrentPage;
            set
            {
                _deviceCurrentPage = value;
                OnPropertyChanged();
                UpdatePagedDevices();
            }
        }

        public int DeviceTotalPages
        {
            get => _deviceTotalPages;
            set { _deviceTotalPages = value; OnPropertyChanged(); }
        }

        public ICommand NextDevicePageCommand { get; }
        public ICommand PreviousDevicePageCommand { get; }

        public int DeviceItemsPerPage
        {
            get => _deviceItemsPerPage;
            set
            {
                if (_deviceItemsPerPage != value)
                {
                    _deviceItemsPerPage = value;
                    OnPropertyChanged();
                    UpdatePagedDevices();
                }
            }
        }

        public double DeviceRowHeight
        {
            get => _deviceRowHeight;
            set
            {
                if (_deviceRowHeight != value)
                {
                    _deviceRowHeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private void ChangeDevicePage(int offset)
        {
            var newPage = DeviceCurrentPage + offset;
            if (newPage >= 1 && newPage <= DeviceTotalPages) DeviceCurrentPage = newPage;
        }

        private void UpdatePagedDevices()
        {
            var filtered = _allDevices.Where(d => string.IsNullOrWhiteSpace(DeviceSearchText) || 
                                                  d.Name.IndexOf(DeviceSearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                  d.Serial.IndexOf(DeviceSearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                                       .ToList();

            DeviceTotalPages = (int)Math.Ceiling((double)filtered.Count / _deviceItemsPerPage);
            if (DeviceTotalPages < 1) DeviceTotalPages = 1;
            
            if (DeviceCurrentPage > DeviceTotalPages) DeviceCurrentPage = DeviceTotalPages;
            
            var paged = filtered.Skip((DeviceCurrentPage - 1) * _deviceItemsPerPage).Take(_deviceItemsPerPage).ToList();
            
            Application.Current.Dispatcher.Invoke(() => {
                _devices.Clear();
                foreach (var d in paged) _devices.Add(d);
                OnPropertyChanged(nameof(DevicesCountText));
            });
        }

        public string DevicesCountText
        {
            get
            {
                var filtered = _allDevices.Where(d => string.IsNullOrWhiteSpace(DeviceSearchText) || 
                                                  d.Name.IndexOf(DeviceSearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                  d.Serial.IndexOf(DeviceSearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                                       .ToList();
                int start = filtered.Any() ? (DeviceCurrentPage - 1) * _deviceItemsPerPage + 1 : 0;
                int end = Math.Min(DeviceCurrentPage * _deviceItemsPerPage, filtered.Count);
                return $"Hiển thị {start} - {end} trong số {filtered.Count} thiết bị";
            }
        }

        public ScriptModel? CurrentScript
        {
            get => _currentScript;
            set 
            { 
                _currentScript = value; 
                OnPropertyChanged(); 
                // Reset selected step when changing script
                SelectedStep = null; 
            }
        }

        private ScriptStepModel? _selectedStep;
        public ScriptStepModel? SelectedStep
        {
            get => _selectedStep;
            set
            {
                _selectedStep = value;
                OnPropertyChanged();
            }
        }

        public AdbDeviceModel? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                _selectedDevice = value;
                OnPropertyChanged();
            }
        }

        public string CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public string CommandInput
        {
            get => _commandInput;
            set
            {
                _commandInput = value;
                OnPropertyChanged();
            }
        }

        public string AdbPath
        {
            get => _adbPath;
            set
            {
                _adbPath = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string LoadingStatus
        {
            get => _loadingStatus;
            set { _loadingStatus = value; OnPropertyChanged(); }
        }

        public string CommandOutput
        {
            get => _commandOutput;
            set
            {
                _commandOutput = value;
                OnPropertyChanged();
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ExecuteCommand { get; }
        public ICommand NavigateCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand RenameDeviceCommand { get; }
        public ICommand BrowseAdbCommand { get; }
        public ICommand ApplySettingsCommand { get; }
        public ICommand RunDashboardScriptCommand { get; }
        public ICommand RunDeviceScriptCommand { get; }
        public ICommand PauseDeviceScriptCommand { get; }
        public ICommand StopDeviceScriptCommand { get; }

        public ObservableCollection<DashboardTaskModel> RunningDashboardTasks => _runningDashboardTasks;

        private async Task InitializeAdb()
        {
            try
            {
                IsLoading = true;
                LoadingStatus = "Đang kiểm tra kết nối ADB...";
                
                await Task.Delay(1000); // Premium feel delay

                var (success, message) = await _adbService.StartServerAsync(AdbPath);
                if (success)
                {
                    CommandOutput += $"{message}\n";
                    LoadingStatus = "Đang tải danh sách thiết bị...";
                    await RefreshDevicesAsync();
                }
                else
                {
                    CommandOutput += $"Failed to start ADB server: {message}\n";
                    CommandOutput += "Please check the ADB path in Settings.\n";
                }

                LoadingStatus = "Đang tải các kịch bản...";
                await Task.Run(() => LoadScripts());
                
                await Task.Delay(500); // Final polish delay
                IsLoading = false;
            }
            catch (Exception ex)
            {
                CommandOutput += $"Unexpected error during ADB initialization: {ex.Message}\n";
                IsLoading = false;
            }
        }
    
        private async Task RefreshDevicesAsync()
        {
            try
            {
                var onlineDevices = await _adbService.GetDevicesAsync();
                var knownDevices = _adbService.GetAllKnownDevices();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _allDevices.Clear();
                    int index = 1;

                    // Group by serial to handle duplicates or merging
                    var onlineMap = onlineDevices.ToDictionary(d => d.Serial);
                    
                    // We want to show ALL known devices
                    foreach (var known in knownDevices)
                    {
                        AdbDeviceModel model;
                        if (onlineMap.TryGetValue(known.Serial, out var onlineData))
                        {
                            model = new AdbDeviceModel(onlineData);
                            model.Status = "Active";
                        }
                        else
                        {
                            // Offline device
                            var offlineData = new AdvancedSharpAdbClient.Models.DeviceData
                            {
                                Serial = known.Serial,
                                Model = known.Model,
                                State = AdvancedSharpAdbClient.Models.DeviceState.Offline
                            };
                            model = new AdbDeviceModel(offlineData);
                            model.Status = "Disconnected";
                        }

                        model.Index = index++;
                        model.CustomName = known.CustomName ?? string.Empty;
                        
                        // Mock data for visual parity
                        var rand = new System.Random(model.Serial.GetHashCode());
                        model.BatteryLevel = rand.Next(5, 100);
                        model.IsCharging = rand.Next(0, 2) == 1;
                        model.ScriptCount = rand.Next(0, 10);
                        model.PhoneNumber = $"+1 (555) {rand.Next(100, 999)}-{rand.Next(1000, 9999)}";

                        _allDevices.Add(model);
                    }
                    TotalDevicesCount = _allDevices.Count;
                    OnlineDevicesCount = _allDevices.Count(d => d.Status == "Active");
                    _allDevicesCollection.Clear();
                    foreach (var d in _allDevices) _allDevicesCollection.Add(d);
                    
                    UpdatePagedDevices();
                });
            }
            catch (Exception ex)
            {
                CommandOutput += $"Error refreshing devices: {ex.Message}\n";
            }
        }

        // Scripting
        private ObservableCollection<ScriptModel> _allScripts = new();
        private ObservableCollection<ScriptModel> _scripts = new();
        private string _scriptSearchText = string.Empty;
        private int _currentPage = 1;
        private int _itemsPerPage = 15;
        private int _totalPages = 1;

        // Sub-ViewModel for Editor
        private ScriptEditorViewModel? _scriptEditor;
        public ScriptEditorViewModel? ScriptEditor
        {
            get => _scriptEditor;
            set { _scriptEditor = value; OnPropertyChanged(); }
        }

        public SchedulerViewModel? Scheduler
        {
            get => _scheduler;
            set { _scheduler = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ScriptModel> AllScripts => _allScripts;

        public ObservableCollection<ScriptModel> Scripts
        {
            get => _scripts;
            set { _scripts = value; OnPropertyChanged(); }
        }

        public string ScriptSearchText
        {
            get => _scriptSearchText;
            set
            {
                _scriptSearchText = value;
                OnPropertyChanged();
                CurrentPage = 1; // Reset to first page on search
                UpdatePagedScripts();
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged();
                UpdatePagedScripts();
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set { _totalPages = value; OnPropertyChanged(); }
        }

        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand CreateScriptCommand { get; }
        public ICommand EditScriptCommand { get; }
        public ICommand DeleteScriptCommand { get; }

        private void LoadScripts()
        {
            var scriptsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts");
            
            if (!Directory.Exists(scriptsDir))
            {
                try { Directory.CreateDirectory(scriptsDir); } catch { return; }
            }

            var files = Directory.GetFiles(scriptsDir, "*.nlp");
            var loadedScripts = new System.Collections.Generic.List<ScriptModel>();
            int i = 1;

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                ScriptModel? model = null;
                try { model = JsonSerializer.Deserialize<ScriptModel>(File.ReadAllText(file)); } catch { }

                if (model == null)
                {
                    model = new ScriptModel { Name = Path.GetFileNameWithoutExtension(fileInfo.Name), Version = "1.0.0", LastModified = fileInfo.LastWriteTime };
                }
                else if (model.LastModified == default) 
                {
                    model.LastModified = fileInfo.LastWriteTime;
                }

                model.Index = i++;
                model.FileName = fileInfo.Name;
                loadedScripts.Add(model);
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                _allScripts.Clear();
                foreach (var script in loadedScripts)
                {
                    _allScripts.Add(script);
                }
                UpdatePagedScripts();
            });
        }

        private void UpdatePagedScripts()
        {
            var filtered = _allScripts.Where(s => string.IsNullOrWhiteSpace(ScriptSearchText) || 
                                                  s.Name.IndexOf(ScriptSearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                                      .ToList();

            TotalPages = (int)Math.Ceiling((double)filtered.Count / _itemsPerPage);
            if (TotalPages < 1) TotalPages = 1;
            
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            
            var paged = filtered.Skip((CurrentPage - 1) * ItemsPerPage).Take(ItemsPerPage).ToList();
            Scripts = new ObservableCollection<ScriptModel>(paged);
        }

        public int ItemsPerPage => _itemsPerPage;
        
        private void ChangePage(int offset)
        {
            var newPage = CurrentPage + offset;
            if (newPage >= 1 && newPage <= TotalPages) CurrentPage = newPage;
        }

        private void SelectAllDevices(bool isSelected) { }
        
        private void RenameDevice(object? parameter)
        {
            if (parameter is AdbDeviceModel device)
            {
                if (device.IsRenaming)
                {
                    _adbService.SetDeviceCustomName(device.Serial, device.CustomName);
                    device.IsRenaming = false;
                }
                else
                {
                    device.IsRenaming = true;
                }
            }
        }

        private async Task ExecuteCommandAsync()
        {
            if (SelectedDevice == null) return;
            var cmd = CommandInput;
            CommandInput = ""; 
            CommandOutput += $"> {cmd}\n";
            var result = await _adbService.ExecuteCommandAsync(SelectedDevice.GetDeviceData(), cmd);
            CommandOutput += result + "\n";
        }

        private void BrowseAdb()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "ADB Executable (adb.exe)|adb.exe|All Files (*.*)|*.*",
                Title = "Select adb.exe"
            };

            if (dialog.ShowDialog() == true) AdbPath = dialog.FileName;
        }

        private async Task ApplySettingsAsync()
        {
            try
            {
                _adbService.SetAdbPath(AdbPath);
                CommandOutput += $"Settings saved. Attempting to start ADB server with: {AdbPath}...\n";
                
                var (success, message) = await _adbService.StartServerAsync(AdbPath);
                if (success)
                {
                    CommandOutput += $"{message}\n";
                    await RefreshDevicesAsync();
                    MessageBox.Show("Settings applied and ADB server is running.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    CommandOutput += $"Error: {message}\n";
                    MessageBox.Show($"Failed to start ADB server.\n\nReason: {message}", "ADB Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                CommandOutput += $"Critical error applying settings: {ex.Message}\n";
                MessageBox.Show($"A critical error occurred: {ex.Message}", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task OpenDeviceAsync(AdbDeviceModel? device)
        {
            if (device != null) await _adbService.OpenDeviceAsync(device.Serial, device.Name, device.Index);
        }

        private void RunDashboardScript(object? parameter)
        {
            if (parameter is ScriptModel script)
            {
                var task = new DashboardTaskModel
                {
                    ScriptName = script.Name,
                    Status = "Khởi tạo...",
                    StartTime = DateTime.Now
                };
                RunningDashboardTasks.Insert(0, task);

                // Run in background
                Task.Run(async () =>
                {
                    int incrementedCount = 0;
                    try
                    {
                        var onlineDevices = Devices.Where(d => d.State == AdvancedSharpAdbClient.Models.DeviceState.Online).ToList();
                        if (!onlineDevices.Any())
                        {
                            Application.Current.Dispatcher.Invoke(() => task.Status = "Không có thiết bị Online");
                            return;
                        }

                        incrementedCount = onlineDevices.Count;
                        Application.Current.Dispatcher.Invoke(() => {
                            RunningThreadsCount += incrementedCount;
                            task.Status = $"Đang chạy ({incrementedCount} máy)...";
                        });
                        
                        // Actual execution of script steps
                        foreach(var step in script.Steps)
                        {
                            Application.Current.Dispatcher.Invoke(() => task.Status = $"Thực hiện: {step.DisplayName}");
                            
                            var tasks = onlineDevices.Select(d => _adbService.ExecuteStepAsync(d.Serial, step)).ToList();
                            await Task.WhenAll(tasks);
                            
                            // Respect a minimum delay between steps if needed, but ExecuteStepAsync might already have it (Pause step)
                            await Task.Delay(100); 
                        }
                        
                        Application.Current.Dispatcher.Invoke(() => {
                            task.Status = "Hoàn thành";
                            RunningThreadsCount -= incrementedCount;
                        });
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() => {
                            task.Status = $"Lỗi: {ex.Message}";
                            if (incrementedCount > 0) RunningThreadsCount -= incrementedCount;
                        });
                    }
                });
            }
        }

        private void RunDeviceScript(object? parameter)
        {
            if (parameter is AdbDeviceModel device && device.SelectedScript != null)
            {
                var script = device.SelectedScript;
                device.ScriptStatus = "Đang chạy...";
                device.IsScriptRunning = true;
                device.IsScriptPaused = false;
                RunningThreadsCount++;

                Task.Run(async () =>
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        foreach (var step in script.Steps)
                        {
                            while (device.IsScriptPaused) await Task.Delay(500);
                            if (!device.IsScriptRunning) break;

                            Application.Current.Dispatcher.Invoke(() => device.ScriptStatus = step.DisplayName);
                            
                            bool success = await _adbService.ExecuteStepAsync(device.Serial, step);
                            if (!success) 
                            {
                                // Optional: decide if stop on error
                                // break;
                            }
                        }
                        sw.Stop();
                        
                        if (device.IsScriptRunning)
                        {
                            var seconds = (int)sw.Elapsed.TotalSeconds;
                            Application.Current.Dispatcher.Invoke(() => {
                                device.ScriptStatus = $"Xong ({seconds}s)";
                                device.IsScriptRunning = false;
                                RunningThreadsCount--;
                            });
                        }
                        else
                        {
                             Application.Current.Dispatcher.Invoke(() => {
                                 device.ScriptStatus = "Đã dừng";
                                 RunningThreadsCount--;
                             });
                        }
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        Application.Current.Dispatcher.Invoke(() => {
                            device.ScriptStatus = $"Lỗi: {ex.Message}";
                            device.IsScriptRunning = false;
                            RunningThreadsCount--;
                        });
                    }
                });
            }
            else if (parameter is AdbDeviceModel)
            {
                MessageBox.Show("Vui lòng chọn kịch bản cho thiết bị này.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void PauseDeviceScript(object? parameter)
        {
            if (parameter is AdbDeviceModel device)
            {
                device.IsScriptPaused = !device.IsScriptPaused;
                if (device.IsScriptPaused) device.ScriptStatus = "Đã tạm dừng";
                else device.ScriptStatus = "Đang tiếp tục...";
            }
        }

        private void StopDeviceScript(object? parameter)
        {
            if (parameter is AdbDeviceModel device && device.IsScriptRunning)
            {
                // Note: The background task will handle the decrement when it sees IsScriptRunning = false
                device.IsScriptRunning = false;
                device.IsScriptPaused = false;
                device.ScriptStatus = "Đang dừng...";
            }
        }

        // Script Editor Methods
        private void CreateNewScript(object? _)
        {
            var newScript = new ScriptModel { Name = "Kịch bản mới", Version = "1.0.0" };
            ScriptEditor = new ScriptEditorViewModel(newScript, Devices, OnScriptSaved);
            CurrentView = "ScriptEditor";
        }

        private void EditScript(object? parameter)
        {
            if (parameter is ScriptModel script)
            {
                ScriptEditor = new ScriptEditorViewModel(script, Devices, OnScriptSaved);
                CurrentView = "ScriptEditor";
            }
        }

        private void DeleteScript(object? parameter)
        {
            if (parameter is ScriptModel script)
            {
                if (MessageBox.Show($"Bạn có chắc chắn muốn xóa kịch bản '{script.Name}'?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    var scriptsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts");
                    // Use actual filename if known, otherwise fall back to name-based guess
                    var fileName = !string.IsNullOrEmpty(script.FileName) ? script.FileName : $"{script.Name}.nlp";
                    var filePath = Path.Combine(scriptsDir, fileName);

                    if (File.Exists(filePath))
                    {
                        try 
                        { 
                            File.Delete(filePath);
                            _allScripts.Remove(script);
                            UpdatePagedScripts();
                        } 
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Lỗi khi xóa file: {ex.Message}");
                        }
                    }
                    else
                    {
                        // Even if file is missing, remove from list to stay in sync
                        _allScripts.Remove(script);
                        UpdatePagedScripts();
                    }
                }
            }
        }

        private void OnScriptSaved()
        {
            LoadScripts();
            CurrentView = "Scripts";
        }

        private void Logout()
        {
             var authService = new AuthService();
             authService.Logout();

             // Restart Application
             var processPath = System.Environment.ProcessPath;
             if (!string.IsNullOrEmpty(processPath))
             {
                 System.Diagnostics.Process.Start(processPath);
             }
             Application.Current.Shutdown();
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
