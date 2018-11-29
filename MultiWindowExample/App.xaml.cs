using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Shell;

namespace MultiWindowExample
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application, ISingleInstanceApp
	{
		private const string Unique = "6bea6fc4-5d9c-4961-b39d-89addcd65a73";

		[STAThread]
		public static void Main()
		{
			Debugger.Launch();
			if (SingleInstance<App>.InitializeAsFirstInstance(Unique))
			{
				var application = new App();

				application.InitializeComponent();
				application.Run();

				// Allow single instance code to perform cleanup operations
				SingleInstance<App>.Cleanup();
			}
		}

		#region ISingleInstanceApp Members
		public bool SignalExternalCommandLineArgs(IList<string> args)
		{
			if (args.Count < 2)
			{
				// Invalid number of arguments
				return true;
			}

			string name = args[1];

			// handle command line arguments of second instance
			Window window = CreeateWindow(name);

			if (window != null)
			{
				// TODO: register with Finsemble
				window.Show();
			}

			return true;
		}

		private static Window CreeateWindow(string name)
		{
			Window window = null;
			switch (name)
			{
				case "Window1":
					{
						window = new Window1();
						break;
					}
				case "Window2":
					{
						window = new Window2();
						break;
					}
				case "Window3":
					{
						window = new Window3();
						break;
					}
				case "Window4":
					{
						window = new Window4();
						break;
					}
				default:
					{
						// Unknown window, ignore
						break;
					}
			}

			return window;
		}
		#endregion

		private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			Debugger.Launch();
			MessageBox.Show("An Unhandled Exception has occurred. Please Check your event Logs.");
		}
	}
}