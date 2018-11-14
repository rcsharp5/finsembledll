using System.Diagnostics;
using System.Windows;

namespace WPFExample
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Debugger.Launch(); // uncomment to launch debugger when executing externally.
            var mainWindow = new MainWindow(e.Args); // send command line arguments to main window.
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Debugger.Launch();
            MessageBox.Show("An Unhandled Exception has occurred. Please Check your event Logs.");
        }
    }
}
