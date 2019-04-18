﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WindowlessExample
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
#if DEBUG
			Debugger.Launch();
#endif

			var mainWindow = new MyClass(e.Args); // send command line arguments to main window.

			this.Exit += (s, e0) => mainWindow.Dispose();
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
#if DEBUG
			Debugger.Launch();
#endif

			MessageBox.Show("An Unhandled Exception has occured. Please Check your event Logs.");
		}
	}
}
