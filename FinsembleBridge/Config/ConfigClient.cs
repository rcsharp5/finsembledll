using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartIQ.Finsemble
{
    public class ConfigClient
    {
        private FinsembleBridge bridge;
        private RouterClient routerClient;
        public ConfigClient(FinsembleBridge bridge)
        {
            this.bridge = bridge;
            this.routerClient = bridge.routerClient;
        }

        public void getValue(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            routerClient.query("configService.getValue", new JObject
            {
                ["field"] = parameters["field"]
            }, new JObject { }, callback);
        }
    }
}
