using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartIQ.Finsemble
{
    public class StorageClient
    {
        private RouterClient routerClient;

        public StorageClient(FinsembleBridge bridge)
        {
            this.routerClient = bridge.routerClient;
        }

        public void get(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            routerClient.query("Storage.get", parameters, new JObject { }, callback);
        }

        public void save(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            routerClient.query("Storage.save", parameters, new JObject { }, callback);
        }
    }
}
