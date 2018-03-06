using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp2
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
            string top = null, left = null, height = null, width = null, componentType = null;
            if (e.Args.Length > 0)
            {

                for (var i = 0; i < e.Args.Length; i++)
                {
                    var argument = e.Args[i].Split(new char[] { '=' }, 2);
                    var argumentName = argument[0];
                    var argumentValue = argument[1];
                    switch (argumentName)
                    {
                        case "top":
                            top = argumentValue;
                            break;
                        case "left":
                            left = argumentValue;
                            break;
                        case "width":
                            width = argumentValue;
                            break;
                        case "height":
                            height = argumentValue;
                            break;
                        case "finsembleWindowName":
                            mainWindowName = argumentValue;
                            break;
                        case "componentType":
                            componentType = argumentValue;
                            break;
                    }


                }
            }
            var mainWindow = new MainWindow(mainWindowName, componentType, top, left, height, width);
        }
    }
}
