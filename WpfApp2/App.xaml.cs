using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace FinsembleWPFDemo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            //base.OnStartup(e);
            string mainWindowName = "";
            string top = null, left = null, height = null, width = null, componentType = null, uuid = null, openfinVersion = "8.56.28.34";
            
            //Debugger.Launch();
            var mainWindow = new MainWindow(e.Args);
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("An Unhandled Exception has occured. Please Check your event Logs.");
        }
    }
}
