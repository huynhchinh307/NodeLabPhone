using NodeLabFarm.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;

namespace NodeLabFarm.ViewModels
{
    public class CreateScriptViewModel : INotifyPropertyChanged
    {
        private ScriptModel _script = new();

        public CreateScriptViewModel()
        {
            AddStepCommand = new RelayCommand(_ => AddStep());
            SaveCommand = new RelayCommand(_ => SaveScript());
            CancelCommand = new RelayCommand(_ => Cancel());

            // Add mock steps to match reference image
            _script.Steps.Add(new ScriptStepModel { Type = StepType.OpenApp, Target = "Instagram", Icon = "PhoneIphone24", Delay = "Every 2 Hours", Timeout = "2 sec" });
            _script.Steps.Add(new ScriptStepModel { Type = StepType.Tap, Target = "560", Value = "1860", Icon = "Fingerprint24", Delay = "3 sec", Timeout = "3 sec" });
            _script.Steps.Add(new ScriptStepModel { Type = StepType.Swipe, Icon = "Hand24", Delay = "5 sec", Timeout = "5 sec" });
            _script.Steps.Add(new ScriptStepModel { Type = StepType.Tap, Target = "100", Value = "200", Icon = "Sparkle24", Delay = "1 min", Timeout = "1 min" });
            _script.Steps.Add(new ScriptStepModel { Type = StepType.Pause, Icon = "Timer24", Delay = "5 min", Timeout = "5 min" });
        }

        public ScriptModel Script
        {
            get => _script;
            set { _script = value; OnPropertyChanged(); }
        }

        public ICommand AddStepCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private void AddStep()
        {
            Script.Steps.Add(new ScriptStepModel { Type = StepType.Tap, Target = "0", Value = "0" });
        }

        private void SaveScript()
        {
            MessageBox.Show($"Script '{Script.Name}' saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Cancel()
        {
            // Navigation back to Dashboard/Devices logic handled by MainViewModel
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
