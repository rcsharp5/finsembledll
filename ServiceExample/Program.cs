using System;
using System.Configuration.Install;
using System.Reflection;
using System.ServiceProcess;

namespace ServiceExample
{
    class Program
    {
        static void Main(String[] args)
        {
            if (Environment.UserInteractive)
            {
                string parameter = string.Concat(args);
                switch (parameter)
                {
                    case "--install":
                        ManagedInstallerClass.InstallHelper(new[] { Assembly.GetExecutingAssembly().Location });
                        break;
                    case "--uninstall":
                        ManagedInstallerClass.InstallHelper(new[] { "/u", Assembly.GetExecutingAssembly().Location });
                        break;
                }
            }
            else
            {
                ServiceBase[] servicesToRun = new ServiceBase[]
                                  {
                              new MyService(args)
                                  };
                ServiceBase.Run(servicesToRun);
            }
        }
    }
}
