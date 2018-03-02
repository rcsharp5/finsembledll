using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartIQ.Finsemble
{
    public class DistributedStoreClient
    {
        FinsembleBridge bridge;
        public DistributedStoreClient(FinsembleBridge bridge)
        {
            this.bridge = bridge;
        }

        public void getStore(JObject parameters, EventHandler<StoreModel> args)
        {
            EventHandler<FinsembleEventArgs> handler = (EventHandler<FinsembleEventArgs>)delegate (object sender, FinsembleEventArgs e) {
                var store = new StoreModel(e.response["data"] as JObject, bridge);
                args.Invoke(this, store);
            };
            bridge.routerClient.query("storeService.getStore", parameters, new JObject { }, handler );
        }



    }
}
