using Wpf.Ui.Controls;
using NodeLabFarm.ViewModels;

namespace NodeLabFarm
{
    public partial class LoginWindow : FluentWindow
    {
        public LoginWindow()
        {
            InitializeComponent();
            var vm = new LoginViewModel();
            this.DataContext = vm;
            vm.RequestClose += (s, e) => 
            {
                this.DialogResult = true;
                this.Close();
            };
        }
    }
}
