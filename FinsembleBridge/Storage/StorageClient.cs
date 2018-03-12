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

        public void Get(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            routerClient.Query("Storage.get", parameters, new JObject { }, callback);
        }

        public void Save(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            routerClient.Query("Storage.save", parameters, new JObject { }, callback);
        }
    }
}
