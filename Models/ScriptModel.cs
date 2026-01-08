using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace NodeLabFarm.Models
{
    public enum StepType
    {
        OpenApp,
        Tap,
        Swipe,
        Pause,
        Type,
        Home,
        Back,
        PressMenu,
        Screenshot,
        ImageSearch,
        SwipeAndCheck,
        ClearText,
        FindText,
        
        SetClipboard,
        GetClipboard,
        Reconnect,
        TransferFile,
        ScreenAction,
        ToggleService,
        ChangeDevice,
        GetPropertyDevice,
        CheckNetwork,
        DumpXml,
        Proxy,
        
        IsOpenApp,
        StartApp,
        StopApp,
        InstallApp,
        UninstallApp,
        IsInstalledApp,
        BackupRestore,
        BackupRestoreDevice,
        ClearDataApp,
        CloseAllApp,

        JavaScript,
        ElementExists,
        PressKey,
        AdbCommand,

        ReadFileText,
        InsertData,
        DeleteData,
        GetLogData,
        SliceVariable,
        IncreaseVariable,
        RegexVariable,
        DataMapping,
        SplitData,
        SortData,
        GetAttribute,
        Random,
        ImapReadMail,
        ReadHotmail,
        RefreshHotmailToken,
        FileAction,
        Generate2FA,

        Excel,
        GoogleSheets,
        GeminiAI,
        ChatGPT,

        RepeatTask,
        Conditions,
        WhileLoop,
        LoopData,
        LoopBreakpoint,

        Start,
        End,
        ResourceStatus,
        HttpRequest,
        BlockGroup,
        Note
    }

    public class ScriptStepModel : INotifyPropertyChanged
    {
        private StepType _type;
        private string _target = string.Empty;
        private string _value = string.Empty;
        private string _delay = "2 sec";
        private string _timeout = "5 sec";
        private string _icon = "Cursor24";
        private string _selectorType = "Coordinates-Position";
        private string _touchType = "Normal";
        private string _swipeMode = "Simple";
        private string _swipeDirection = "Up";
        private string _startX = "0";
        private string _endX = "0";
        private string _startY = "0";
        private string _endY = "0";
        private string _duration = "300";

        public StepType Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string Target
        {
            get => _target;
            set { _target = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string Delay
        {
            get => _delay;
            set { _delay = value; OnPropertyChanged(); }
        }

        public string Timeout
        {
            get => _timeout;
            set { _timeout = value; OnPropertyChanged(); }
        }

        public string Icon
        {
            get => _icon;
            set { _icon = value; OnPropertyChanged(); }
        }

        public string SelectorType
        {
            get => _selectorType;
            set { _selectorType = value; OnPropertyChanged(); }
        }

        public string TouchType
        {
            get => _touchType;
            set { _touchType = value; OnPropertyChanged(); }
        }

        public string SwipeMode
        {
            get => _swipeMode;
            set { _swipeMode = value; OnPropertyChanged(); }
        }

        public string SwipeDirection
        {
            get => _swipeDirection;
            set { _swipeDirection = value; OnPropertyChanged(); }
        }

        public string StartX
        {
            get => _startX;
            set { _startX = value; OnPropertyChanged(); }
        }

        public string EndX
        {
            get => _endX;
            set { _endX = value; OnPropertyChanged(); }
        }

        public string StartY
        {
            get => _startY;
            set { _startY = value; OnPropertyChanged(); }
        }

        public string EndY
        {
            get => _endY;
            set { _endY = value; OnPropertyChanged(); }
        }

        public string Duration
        {
            get => _duration;
            set { _duration = value; OnPropertyChanged(); }
        }

        public string DisplayName => Type switch
        {
            StepType.OpenApp => $"Open App \"{Target}\"",
            StepType.Tap => $"Touch at {Target}",
            StepType.Swipe => "Swipe/Scroll",
            StepType.Pause => "Delay",
            StepType.Type => $"Type \"{Value}\"",
            StepType.Home => "Press Home",
            StepType.Back => "Press Back",
            StepType.PressMenu => "Press Menu",
            StepType.Screenshot => "Screenshot",
            StepType.ImageSearch => $"Find Image \"{Target}\"",
            StepType.SwipeAndCheck => "Swipe & Check",
            StepType.ClearText => "Clear Text",
            StepType.FindText => $"Find Text \"{Value}\"",
            
            StepType.SetClipboard => $"Set Clipboard \"{Value}\"",
            StepType.GetClipboard => "Get Clipboard",
            StepType.Reconnect => "Reconnect ADB",
            StepType.TransferFile => $"Transfer File: {Value}",
            StepType.ScreenAction => "Screen Action",
            StepType.ToggleService => "Toggle Service",
            StepType.ChangeDevice => "Change Device",
            StepType.GetPropertyDevice => "Get Device Property",
            StepType.CheckNetwork => "Check Network",
            StepType.DumpXml => "Dump XML",
            StepType.Proxy => $"Set Proxy: {Value}",
            
            StepType.IsOpenApp => $"Is App Open: {Target}",
            StepType.StartApp => $"Start App \"{Target}\"",
            StepType.StopApp => $"Stop App \"{Target}\"",
            StepType.InstallApp => $"Install App \"{Target}\"",
            StepType.UninstallApp => $"Uninstall App \"{Target}\"",
            StepType.IsInstalledApp => $"Is App Installed: {Target}",
            StepType.BackupRestore => $"Backup/Restore App: {Target}",
            StepType.BackupRestoreDevice => "Backup/Restore Device",
            StepType.ClearDataApp => $"Clear App Data: {Target}",
            StepType.CloseAllApp => "Close All Apps",

            StepType.JavaScript => "JavaScript Code",
            StepType.ElementExists => $"Element Exists: {Target}",
            StepType.PressKey => $"Press Key: {Value}",
            StepType.AdbCommand => $"ADB: {Value}",

            StepType.ReadFileText => $"Read File: {Target}",
            StepType.InsertData => $"Insert Data: {Target}",
            StepType.DeleteData => $"Delete Data: {Target}",
            StepType.GetLogData => "Get Log Data",
            StepType.SliceVariable => $"Slice Variable: {Target}",
            StepType.IncreaseVariable => $"Increase: {Target}",
            StepType.RegexVariable => $"RegEx: {Target}",
            StepType.DataMapping => "Data Mapping",
            StepType.SplitData => $"Split Data: {Target}",
            StepType.SortData => "Sort Data",
            StepType.GetAttribute => $"Get Attribute: {Target}",
            StepType.Random => "Random",
            StepType.ImapReadMail => "IMAP Read Mail",
            StepType.ReadHotmail => "Read Hotmail",
            StepType.RefreshHotmailToken => "Refresh Hotmail Token",
            StepType.FileAction => "File Action",
            StepType.Generate2FA => "Generate 2FA",

            StepType.Excel => $"Excel: {Target}",
            StepType.GoogleSheets => $"Google Sheets: {Target}",
            StepType.GeminiAI => "Gemini AI",
            StepType.ChatGPT => "Chat GPT",

            StepType.RepeatTask => $"Repeat: {Value} times",
            StepType.Conditions => $"If: {Target}",
            StepType.WhileLoop => $"While: {Target}",
            StepType.LoopData => "Loop Data",
            StepType.LoopBreakpoint => "Loop Breakpoint",

            StepType.Start => "Start",
            StepType.End => "End",
            StepType.ResourceStatus => "Resource Status",
            StepType.HttpRequest => "HTTP Request",
            StepType.BlockGroup => $"Block Group: {Value}",
            StepType.Note => $"Note: {Value}",
            
            _ => "Action"
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ScriptModel : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private int _index;
        private string _name = "Kịch bản mới";
        private string _version = "1.0.0";
        private DateTime _lastModified = DateTime.Now;
        private string _description = string.Empty;
        private string _fileName = string.Empty;
        private ObservableCollection<ScriptStepModel> _steps = new();

        [JsonIgnore]
        public string FileName
        {
            get => _fileName;
            set { _fileName = value; OnPropertyChanged(); }
        }

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public int Index
        {
            get => _index;
            set { _index = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Version
        {
            get => _version;
            set { _version = value; OnPropertyChanged(); }
        }

        public DateTime LastModified
        {
            get => _lastModified;
            set { _lastModified = value; OnPropertyChanged(); }
        }

        public string LastModifiedDisplay => LastModified.ToString("dd/MM/yyyy HH:mm");

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ScriptStepModel> Steps
        {
            get => _steps;
            set { _steps = value; OnPropertyChanged(); }
        }

        public ObservableCollection<VariableModel> Variables { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
