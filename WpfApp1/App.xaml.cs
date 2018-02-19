using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp1
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
            if (e.Args.Length > 0)
            {
                mainWindowName = e.Args[0].Split('=')[1];
            }
            var mainWindow = new MainWindow(mainWindowName);
        }
    }
}
