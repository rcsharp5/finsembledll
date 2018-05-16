using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace ServiceExample
{
    class MyServiceInstaller : Installer
    {
        private ServiceProcessInstaller processInstaller;
        private ServiceInstaller serviceInstaller;

        public MyServiceInstaller()
        {
            processInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();

            processInstaller.Account = ServiceAccount.LocalSystem;
            serviceInstaller.StartType = ServiceStartMode.Manual;
            serviceInstaller.ServiceName = "MyService"; //must match CronService.ServiceName

            Installers.Add(serviceInstaller);
            Installers.Add(processInstaller);
        }
    }
}
