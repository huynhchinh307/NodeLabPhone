using NodeLabFarm.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NodeLabFarm.ViewModels
{
    using NodeLabFarm.Services;
    using System.Linq;
    using System.Collections.Generic;
    using System.Windows.Data;
    using System.Collections.Specialized;
    using System.ComponentModel;
    public class ScriptEditorViewModel : INotifyPropertyChanged
    {
        private ScriptModel? _currentScript;
        private ScriptStepModel? _selectedStep;
        private AdbDeviceModel? _selectedTestDevice;
        private readonly Action _onScriptSaved;
        private readonly IAdbService _adbService;
        private System.Windows.Media.Imaging.BitmapSource? _screenImage;
        private string _inspectInfo = "Click để khóa, Rê để soi";
        private bool _isCapturing;
        private List<Dictionary<string, string>>? _hierarchyNodes;
        private Dictionary<string, string>? _hoveredElement;
        private bool _isInspectLocked;
        private string _selectedXPath = "";
        private string _selectedCoords = "";
        public ObservableCollection<AdbDeviceModel> Devices { get; }
        public ICollectionView OnlineDevices { get; }
        public ObservableCollection<string> Logs { get; } = new();
        public ObservableCollection<VariableModel> Variables => CurrentScript?.Variables ?? new();

        public ScriptEditorViewModel(ScriptModel script, ObservableCollection<AdbDeviceModel> devices, Action onScriptSaved)
        {
            CurrentScript = script;
            Devices = devices;
            _onScriptSaved = onScriptSaved;
            _adbService = new AdbService();

            OnlineDevices = new ListCollectionView(Devices);
            OnlineDevices.Filter = d => (d as AdbDeviceModel)?.State == AdvancedSharpAdbClient.Models.DeviceState.Online;

            AddStepCommand = new RelayCommand(AddStepToScript);
            RemoveStepCommand = new RelayCommand(RemoveStepFromScript);
            SelectStepCommand = new RelayCommand(step => SelectedStep = step as ScriptStepModel);
            CloseStepSettingsCommand = new RelayCommand(_ => SelectedStep = null);
            SaveScriptCommand = new RelayCommand(SaveScript);
            
            RunScriptCommand = new RelayCommand(RunScript);
            StopScriptCommand = new RelayCommand(StopScript);
            RunStepCommand = new RelayCommand(RunStep);
            ViewDeviceCommand = new RelayCommand(ViewDevice);
            ClearLogsCommand = new RelayCommand(_ => Logs.Clear());
            AddVariableCommand = new RelayCommand(_ => Variables.Add(new VariableModel { Key = "NewVar", Value = "Value" }));
            RemoveVariableCommand = new RelayCommand(v => { if (v is VariableModel vm) Variables.Remove(vm); });
            CaptureScreenCommand = new RelayCommand(async _ => await CaptureScreenAsync());
            ScreenClickCommand = new RelayCommand(p => HandleScreenClick(p));
            CopyInspectCommand = new RelayCommand(_ => {
                if (!string.IsNullOrEmpty(SelectedXPath)) {
                    Clipboard.SetText(SelectedXPath);
                    Logs.Add($"[{DateTime.Now:HH:mm:ss}] Đã sao chép XPath.");
                }
            });
            CopyCoordsCommand = new RelayCommand(_ => {
                if (!string.IsNullOrEmpty(SelectedCoords)) {
                    Clipboard.SetText(SelectedCoords);
                    Logs.Add($"[{DateTime.Now:HH:mm:ss}] Đã sao chép tọa độ: {SelectedCoords}");
                }
            });
            ScreenHoverCommand = new RelayCommand(p => HandleScreenHover(p));

            _ = StartScreenMonitoring();

            if (!OnlineDevices.IsEmpty) SelectedTestDevice = OnlineDevices.Cast<AdbDeviceModel>().FirstOrDefault();

            // Initial status log
            Logs.Add($"[{DateTime.Now:HH:mm:ss}] Sẵn sàng chỉnh sửa kịch bản.");

            if (CurrentScript != null && CurrentScript.Variables.Count == 0)
            {
                CurrentScript.Variables.Add(new VariableModel { Key = "DeviceID", Value = "None" });
            }
        }

        public ScriptModel? CurrentScript
        {
            get => _currentScript;
            set
            {
                _currentScript = value;
                OnPropertyChanged();
            }
        }

        public ScriptStepModel? SelectedStep
        {
            get => _selectedStep;
            set
            {
                _selectedStep = value;
                OnPropertyChanged();
            }
        }

        public AdbDeviceModel? SelectedTestDevice
        {
            get => _selectedTestDevice;
            set
            {
                _selectedTestDevice = value;
                OnPropertyChanged();
                _ = CaptureScreenAsync();
            }
        }

        public System.Windows.Media.Imaging.BitmapSource? ScreenImage
        {
            get => _screenImage;
            set { _screenImage = value; OnPropertyChanged(); }
        }

        public string InspectInfo
        {
            get => _inspectInfo;
            set { _inspectInfo = value; OnPropertyChanged(); }
        }

        public Dictionary<string, string>? HoveredElement
        {
            get => _hoveredElement;
            set { _hoveredElement = value; OnPropertyChanged(); }
        }

        public bool IsInspectLocked
        {
            get => _isInspectLocked;
            set { _isInspectLocked = value; OnPropertyChanged(); }
        }

        public string SelectedXPath
        {
            get => _selectedXPath;
            set { _selectedXPath = value; OnPropertyChanged(); }
        }

        public string SelectedCoords
        {
            get => _selectedCoords;
            set { _selectedCoords = value; OnPropertyChanged(); }
        }

        private double _rectX, _rectY, _rectWidth, _rectHeight;
        public double RectX { get => _rectX; set { _rectX = value; OnPropertyChanged(); } }
        public double RectY { get => _rectY; set { _rectY = value; OnPropertyChanged(); } }
        public double RectWidth { get => _rectWidth; set { _rectWidth = value; OnPropertyChanged(); } }
        public double RectHeight { get => _rectHeight; set { _rectHeight = value; OnPropertyChanged(); } }

        public ICommand AddStepCommand { get; }
        public ICommand RemoveStepCommand { get; }
        public ICommand SelectStepCommand { get; }
        public ICommand CloseStepSettingsCommand { get; }
        public ICommand SaveScriptCommand { get; }
        public ICommand RunScriptCommand { get; }
        public ICommand StopScriptCommand { get; }
        public ICommand RunStepCommand { get; }
        public ICommand ViewDeviceCommand { get; }
        public ICommand ClearLogsCommand { get; }
        public ICommand AddVariableCommand { get; }
        public ICommand RemoveVariableCommand { get; }
        public ICommand CaptureScreenCommand { get; }
        public ICommand ScreenClickCommand { get; }
        public ICommand ScreenHoverCommand { get; }
        public ICommand CopyInspectCommand { get; }
        public ICommand CopyCoordsCommand { get; }

        private void AddStepToScript(object? parameter)
        {
            if (CurrentScript == null || parameter is not string typeStr) return;

            if (Enum.TryParse<StepType>(typeStr, out var type))
            {
                var newStep = new ScriptStepModel { Type = type };
                
                // Set defaults based on type
                if (type == StepType.Tap) { newStep.Target = "540"; newStep.Value = "1000"; newStep.Icon = "HandRight24"; newStep.SelectorType = "Coordinates-Position"; newStep.TouchType = "Normal"; }
                else if (type == StepType.Swipe) { newStep.Icon = "ArrowSwap24"; newStep.SwipeMode = "Simple"; newStep.SwipeDirection = "Up"; }
                else if (type == StepType.Type) { newStep.Icon = "Keyboard24"; }
                else if (type == StepType.OpenApp) { newStep.Target = "com.package.name"; newStep.Icon = "Apps24"; }
                else if (type == StepType.Pause) { newStep.Icon = "Timer24"; }
                else if (type == StepType.Home) { newStep.Icon = "Home24"; }
                else if (type == StepType.Back) { newStep.Icon = "ArrowLeft24"; }

                else if (type == StepType.PressMenu) { newStep.Icon = "Navigation24"; }
                else if (type == StepType.Screenshot) { newStep.Icon = "Camera24"; }
                // Image Search icon fallback
                else if (type == StepType.ImageSearch) { newStep.Icon = "Image24"; newStep.Target = "image.png"; }
                else if (type == StepType.SwipeAndCheck) { newStep.Icon = "Eye24"; }
                else if (type == StepType.ClearText) { newStep.Icon = "Eraser24"; }
                else if (type == StepType.FindText) { newStep.Icon = "Search24"; newStep.Value = "Text to find"; }
                
                else if (type == StepType.SetClipboard) { newStep.Icon = "ClipboardPaste24"; newStep.Value = "Text"; }
                else if (type == StepType.GetClipboard) { newStep.Icon = "ClipboardLetter24"; }
                else if (type == StepType.Reconnect) { newStep.Icon = "ArrowSync24"; }
                else if (type == StepType.TransferFile) { newStep.Icon = "FolderArrowRight24"; newStep.Value = "/sdcard/file"; }
                else if (type == StepType.ScreenAction) { newStep.Icon = "Desktop24"; }
                else if (type == StepType.ToggleService) { newStep.Icon = "Settings24"; }
                else if (type == StepType.ChangeDevice) { newStep.Icon = "PhoneArrowRight24"; }
                else if (type == StepType.GetPropertyDevice) { newStep.Icon = "Info24"; }
                else if (type == StepType.CheckNetwork) { newStep.Icon = "Wifi124"; }
                else if (type == StepType.DumpXml) { newStep.Icon = "DocumentData24"; }
                else if (type == StepType.Proxy) { newStep.Icon = "Shield24"; newStep.Value = "127.0.0.1:8080"; }
                
                else if (type == StepType.IsOpenApp) { newStep.Icon = "Apps24"; newStep.Target = "com.pkg"; }
                else if (type == StepType.StartApp) { newStep.Icon = "Play24"; newStep.Target = "com.pkg"; }
                else if (type == StepType.StopApp) { newStep.Icon = "Stop24"; newStep.Target = "com.pkg"; }
                else if (type == StepType.InstallApp) { newStep.Icon = "ArrowDownload24"; newStep.Target = "path/to.apk"; }
                else if (type == StepType.UninstallApp) { newStep.Icon = "Delete24"; newStep.Target = "com.pkg"; }
                else if (type == StepType.IsInstalledApp) { newStep.Icon = "QuestionCircle24"; newStep.Target = "com.pkg"; }
                else if (type == StepType.BackupRestore) { newStep.Icon = "ArrowUndo24"; }
                else if (type == StepType.BackupRestoreDevice) { newStep.Icon = "Phone24"; }
                else if (type == StepType.ClearDataApp) { newStep.Icon = "Eraser24"; newStep.Target = "com.pkg"; }
                else if (type == StepType.CloseAllApp) { newStep.Icon = "DismissCircle24"; }

                else if (type == StepType.JavaScript) { newStep.Icon = "Code24"; newStep.Value = "// JS Code here"; }
                else if (type == StepType.ElementExists) { newStep.Icon = "Eye24"; newStep.Target = "id/element"; }
                else if (type == StepType.PressKey) { newStep.Icon = "Keyboard24"; newStep.Value = "66"; } // 66 is Enter
                else if (type == StepType.AdbCommand) { newStep.Icon = "Code24"; newStep.Value = "shell am force-stop com.pkg"; }

                else if (type == StepType.ReadFileText) { newStep.Icon = "DocumentText24"; }
                else if (type == StepType.InsertData) { newStep.Icon = "Add24"; }
                else if (type == StepType.DeleteData) { newStep.Icon = "Delete24"; }
                else if (type == StepType.GetLogData) { newStep.Icon = "Search24"; }
                else if (type == StepType.SliceVariable) { newStep.Icon = "ArrowSync24"; }
                else if (type == StepType.IncreaseVariable) { newStep.Icon = "Add24"; }
                else if (type == StepType.RegexVariable) { newStep.Icon = "Search24"; }
                else if (type == StepType.DataMapping) { newStep.Icon = "ArrowSwap24"; }
                else if (type == StepType.SplitData) { newStep.Icon = "ArrowSync24"; }
                else if (type == StepType.SortData) { newStep.Icon = "Search24"; }
                else if (type == StepType.GetAttribute) { newStep.Icon = "Key24"; }
                else if (type == StepType.Random) { newStep.Icon = "Key24"; }
                else if (type == StepType.ImapReadMail) { newStep.Icon = "Mail24"; }
                else if (type == StepType.ReadHotmail) { newStep.Icon = "Mail24"; }
                else if (type == StepType.RefreshHotmailToken) { newStep.Icon = "ArrowSync24"; }
                else if (type == StepType.FileAction) { newStep.Icon = "Folder24"; }
                else if (type == StepType.Generate2FA) { newStep.Icon = "Key24"; }

                else if (type == StepType.Excel) { newStep.Icon = "Document24"; }
                else if (type == StepType.GoogleSheets) { newStep.Icon = "Document24"; }
                else if (type == StepType.GeminiAI) { newStep.Icon = "Globe24"; }
                else if (type == StepType.ChatGPT) { newStep.Icon = "QuestionCircle24"; }

                else if (type == StepType.RepeatTask) { newStep.Icon = "ArrowRepeatAll24"; newStep.Value = "5"; }
                else if (type == StepType.Conditions) { newStep.Icon = "Navigation24"; }
                else if (type == StepType.WhileLoop) { newStep.Icon = "ArrowSync24"; }
                else if (type == StepType.LoopData) { newStep.Icon = "ArrowRepeatAll24"; }
                else if (type == StepType.LoopBreakpoint) { newStep.Icon = "Stop24"; }

                else if (type == StepType.Start) { newStep.Icon = "PlayCircle24"; }
                else if (type == StepType.End) { newStep.Icon = "Stop24"; }
                else if (type == StepType.ResourceStatus) { newStep.Icon = "DataBarHorizontal24"; } // Or similar status icon
                else if (type == StepType.HttpRequest) { newStep.Icon = "Globe24"; }
                else if (type == StepType.BlockGroup) { newStep.Icon = "Group24"; newStep.Value = "Group Name"; }
                else if (type == StepType.Note) { newStep.Icon = "Notepad24"; newStep.Value = "Enter note here..."; }

                CurrentScript.Steps.Add(newStep);
            }
        }

        private void RemoveStepFromScript(object? parameter)
        {
            if (CurrentScript == null || parameter is not ScriptStepModel step) return;
            CurrentScript.Steps.Remove(step);
            if (SelectedStep == step) SelectedStep = null;
        }

        private void SaveScript(object? _)
        {
            if (CurrentScript == null) return;

            var result = MessageBox.Show(
                $"Bạn có chắc muốn lưu kịch bản '{CurrentScript.Name}' (v{CurrentScript.Version}) không?", 
                "Xác nhận lưu kịch bản", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                CurrentScript.LastModified = DateTime.Now;
                var fileName = $"{CurrentScript.Name}_v{CurrentScript.Version}.nlp";
                fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
                
                var scriptsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts");
                if (!Directory.Exists(scriptsDir)) Directory.CreateDirectory(scriptsDir);

                var filePath = Path.Combine(scriptsDir, fileName);

                // GHOST FILE FIX: If we have an old filename and it's different from the new one, delete the old file
                if (!string.IsNullOrEmpty(CurrentScript.FileName) && CurrentScript.FileName != fileName)
                {
                    var oldPath = Path.Combine(scriptsDir, CurrentScript.FileName);
                    if (File.Exists(oldPath))
                    {
                        try { File.Delete(oldPath); } catch { }
                    }
                }

                var json = JsonSerializer.Serialize(CurrentScript, new JsonSerializerOptions { WriteIndented = true });
                
                File.WriteAllText(filePath, json);
                CurrentScript.FileName = fileName;
                
                MessageBox.Show("Lưu kịch bản thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Invoke callback to refresh list and navigate
                _onScriptSaved?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu kịch bản: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RunScript(object? _)
        {
            if (SelectedTestDevice == null)
            {
                MessageBox.Show("Vui lòng chọn thiết bị để test!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            MessageBox.Show($"Đang chạy kịch bản trên thiết bị: {SelectedTestDevice.Serial}", "Run Script", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void StopScript(object? _)
        {
            MessageBox.Show("Đã dừng kịch bản!", "Stop Script", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void RunStep(object? parameter)
        {
            if (SelectedTestDevice == null)
            {
                MessageBox.Show("Vui lòng chọn thiết bị để test!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (parameter is ScriptStepModel step)
            {
                Logs.Add($"[{DateTime.Now:HH:mm:ss}] Đang chạy lệnh: {step.DisplayName}...");
                bool success = await _adbService.ExecuteStepAsync(SelectedTestDevice.Serial, step);
                if (success) Logs.Add($"[{DateTime.Now:HH:mm:ss}] Lệnh hoàn tất.");
                else Logs.Add($"[{DateTime.Now:HH:mm:ss}] Lỗi khi thực hiện lệnh.");
            }
        }

        private async Task StartScreenMonitoring()
        {
            while (true)
            {
                if (SelectedTestDevice != null && !_isCapturing)
                {
                    var img = await _adbService.GetScreenshotAsync(SelectedTestDevice.Serial);
                    if (img != null) ScreenImage = img;
                }
                await Task.Delay(1000);
            }
        }

        private async Task CaptureScreenAsync()
        {
            if (SelectedTestDevice == null || _isCapturing) return;
            _isCapturing = true;
            try
            {
                InspectInfo = "Đang quét màn hình & XML...";
                var img = await _adbService.GetScreenshotAsync(SelectedTestDevice.Serial);
                if (img != null) ScreenImage = img;
                
                _hierarchyNodes = await _adbService.GetUIHierarchyAsync(SelectedTestDevice.Serial);
                InspectInfo = "Đã cập nhật ảnh & dữ liệu XML. Rê chuột để soi.";
            }
            finally { _isCapturing = false; }
        }

        private void HandleScreenHover(object? parameter)
        {
            if (IsInspectLocked || parameter is not Point p || _hierarchyNodes == null) return;
            
            int x = (int)p.X;
            int y = (int)p.Y;

            Dictionary<string, string>? target = null;
            int minArea = int.MaxValue;
            double rx = 0, ry = 0, rw = 0, rh = 0;

            foreach (var node in _hierarchyNodes)
            {
                if (node.TryGetValue("bounds", out var bounds))
                {
                    var m = System.Text.RegularExpressions.Regex.Matches(bounds, @"\d+");
                    if (m.Count >= 4)
                    {
                        int x1 = int.Parse(m[0].Value);
                        int y1 = int.Parse(m[1].Value);
                        int x2 = int.Parse(m[2].Value);
                        int y2 = int.Parse(m[3].Value);

                        if (x >= x1 && x <= x2 && y >= y1 && y <= y2)
                        {
                            int area = (x2 - x1) * (y2 - y1);
                            if (area <= minArea) // Use <= to prefer smaller nested nodes if areas are same
                            {
                                minArea = area;
                                target = node;
                                rx = x1; ry = y1; rw = x2 - x1; rh = y2 - y1;
                            }
                        }
                    }
                }
            }

            if (target != null)
            {
                RectX = rx; RectY = ry; RectWidth = rw; RectHeight = rh;
                HoveredElement = target;
                
                string tag = target.GetValueOrDefault("class", "node");
                tag = tag.Contains(".") ? tag.Split('.').Last() : tag;
                
                string attr = target.ContainsKey("resource-id") && !string.IsNullOrEmpty(target["resource-id"]) ? $"[@resource-id='{target["resource-id"]}']" : 
                             (target.ContainsKey("text") && !string.IsNullOrEmpty(target["text"]) ? $"[@text='{target["text"]}']" : "");

                SelectedXPath = $"//{tag}{attr}";
                SelectedCoords = $"{x},{y}";
                InspectInfo = $"X:{x}, Y:{y} | XPath: {SelectedXPath}";
            }
            else
            {
                RectWidth = 0;
                SelectedCoords = $"{x},{y}";
                SelectedXPath = "";
                InspectInfo = $"X:{x}, Y:{y} | (Không có phần tử)";
            }
        }

        private void HandleScreenClick(object? parameter)
        {
            IsInspectLocked = !IsInspectLocked;
            
            if (IsInspectLocked)
            {
                InspectInfo = "(LOCKED) " + InspectInfo;
            }
            else
            {
                // Trigger a hover update to clear the locked status text
                if (parameter is Point p) HandleScreenHover(p);
            }
        }

        private async void ViewDevice(object? _)
        {
            if (SelectedTestDevice == null) return;
            await _adbService.OpenDeviceAsync(SelectedTestDevice.Serial, SelectedTestDevice.Name, SelectedTestDevice.Index);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
