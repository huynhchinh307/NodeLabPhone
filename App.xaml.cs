using System.Configuration;
using System.Data;
using System.Windows;

namespace NodeLabFarm;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        try { System.IO.File.WriteAllText("debug_ctor.txt", "App Ctor reached"); } catch {}
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        string error = $"Unhandled Logic Exception: {e.Exception.Message}\n\n{e.Exception.StackTrace}";
        if (e.Exception.InnerException != null)
        {
            error += $"\n\nInner: {e.Exception.InnerException.Message}";
        }
        System.IO.File.WriteAllText("crash.txt", error);
        MessageBox.Show(error, "Runtime Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true; 
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            System.IO.File.WriteAllText("debug_startup.txt", "OnStartup begin");
            
            var authService = new NodeLabFarm.Services.AuthService();
            if (authService.IsLoggedIn())
            {
                var mainWindow = new MainWindow();
                this.MainWindow = mainWindow;
                mainWindow.Show();
                System.IO.File.WriteAllText("debug_startup_success.txt", "OnStartup success (Auto-Login)");
                return;
            }

            // Prevent auto-shutdown when LoginWindow closes
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            // Show Login Window
            var loginWindow = new LoginWindow();
            bool? result = loginWindow.ShowDialog();

            if (result == true)
            {
                var mainWindow = new MainWindow();
                this.MainWindow = mainWindow;
                mainWindow.Show();
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                System.IO.File.WriteAllText("debug_startup_success.txt", "OnStartup success");
            }
            else
            {
                Shutdown();
            }
        }
        catch (Exception ex)
        {
            string error = $"Startup failed: {ex.Message}\n\n{ex.StackTrace}";
            if (ex.InnerException != null)
            {
                error += $"\n\nInner Exception: {ex.InnerException.Message}";
            }
            System.IO.File.WriteAllText("crash.txt", error);
            MessageBox.Show(error, "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}

