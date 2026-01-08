using System.Windows.Controls;

namespace NodeLabFarm.Views
{
    public partial class ScriptEditorView : UserControl
    {
        public ScriptEditorView()
        {
            InitializeComponent();
        }
        private void Screen_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is not NodeLabFarm.ViewModels.MainViewModel mvm || mvm.ScriptEditor == null) return;
            var vm = mvm.ScriptEditor;
            
            if (vm.ScreenImage == null) return;

            var pos = e.GetPosition(PreviewImage);
            
            // Calculate real coordinates based on image resolution vs display size
            double actualWidth = PreviewImage.ActualWidth;
            double actualHeight = PreviewImage.ActualHeight;
            
            double pixelWidth = vm.ScreenImage.PixelWidth;
            double pixelHeight = vm.ScreenImage.PixelHeight;

            int realX = (int)(pos.X * pixelWidth / actualWidth);
            int realY = (int)(pos.Y * pixelHeight / actualHeight);

            vm.ScreenClickCommand.Execute(new System.Windows.Point(realX, realY));
        }

        private void PreviewContainer_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (DataContext is not NodeLabFarm.ViewModels.MainViewModel mvm || mvm.ScriptEditor == null) return;
            var vm = mvm.ScriptEditor;
            if (vm.ScreenImage == null) return;

            var pos = e.GetPosition(PreviewImage);
            double actualWidth = PreviewImage.ActualWidth;
            double actualHeight = PreviewImage.ActualHeight;
            
            if (actualWidth <= 0 || actualHeight <= 0) return;

            int realX = (int)(pos.X * vm.ScreenImage.PixelWidth / actualWidth);
            int realY = (int)(pos.Y * vm.ScreenImage.PixelHeight / actualHeight);

            vm.ScreenHoverCommand.Execute(new System.Windows.Point(realX, realY));
        }
    }
}
