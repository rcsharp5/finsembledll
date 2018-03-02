using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartIQ.Finsemble
{
    public class LauncherClient
    {
        private FinsembleBridge bridge;
        private RouterClient routerClient;
        private WindowClient windowClient;

        public LauncherClient(FinsembleBridge bridge)
        {
            this.bridge = bridge;
            routerClient = bridge.routerClient;
            windowClient = bridge.windowClient;
        }

        public void showWindow(JObject windowIdentifier, JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            parameters["windowIdentifier"] = windowIdentifier;
            parameters["relativeWindow"] = windowClient.windowIdentifier;
            routerClient.query("Launcher.showWindow", parameters, new JObject { }, callback);
        }

        public void spawn(string component, JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            parameters["launchingWindow"] = windowClient.windowIdentifier;
            parameters["component"] = component;
            routerClient.query("Launcher.spawn", parameters, new JObject { }, callback);
        }
        
    }
}
