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

        /// <summary>
        /// This returns a StoreModel which is used to get and set values on a global store and create listeners on specific values. Only global stores are supported.
        /// </summary>
        /// <example>
        /// <code>
        /// bridge.distributedStoreClient.GetStore(new JObject {
        ///     ["store"] = "storeName",
        ///     ["global"] = true
        /// }, (sender, store) {
        ///     // Do something with the store.
        /// })
        /// </code>
        /// </example>
        /// <param name="parameters"></param>
        /// <param name="args"></param>
        public void GetStore(JObject parameters, EventHandler<StoreModel> args)
        {
            EventHandler<FinsembleEventArgs> handler = (EventHandler<FinsembleEventArgs>)delegate (object sender, FinsembleEventArgs e) {
                var store = new StoreModel(e.response["data"] as JObject, bridge);
                args.Invoke(this, store);
            };
            bridge.routerClient.Query("storeService.getStore", parameters, new JObject { }, handler );
        }



    }
}
