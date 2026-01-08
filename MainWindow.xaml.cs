using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Wpf.Ui.Controls;

namespace NodeLabFarm;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        UpdateItemsPerPage();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        UpdateItemsPerPage();
    }

        private void UpdateItemsPerPage()
        {
            if (this.DataContext is ViewModels.MainViewModel vm)
            {
                if (WindowState == WindowState.Maximized)
                {
                    vm.DeviceItemsPerPage = 10;
                    vm.DeviceRowHeight = 55;
                }
                else
                {
                    vm.DeviceItemsPerPage = 5;
                    vm.DeviceRowHeight = 75;
                }
            }
        }
}
