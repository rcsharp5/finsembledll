using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartIQ.Finsemble
{
    public class StoreModel
    {
        public string name { private set; get; }
        public bool global { private set; get; }
        private JObject values;
        private JObject clients;
        private RouterClient routerClient;
        private Dictionary<string, EventHandler<FinsembleEventArgs>> storeEventListeners = new Dictionary<string, EventHandler<FinsembleEventArgs>>();

        public StoreModel(JObject parameters, FinsembleBridge bridge)
        {
            //store, name, global, values, clients
            this.name = (string)parameters["name"];
            this.global = (bool)parameters["global"];
            this.values = parameters["values"] as JObject; // these come back but are currently not used for global stores.
            this.clients = parameters["clients"] as JObject;
            routerClient = bridge.routerClient;
        }

        public void setValue(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            var data = new JObject
            {
                ["store"] = name,
                ["field"] = (string)parameters["field"],
                ["value"] = parameters["value"]
            };
            routerClient.query("storeService.setValue", data, new JObject { }, callback);
        }

        public void getValue(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            var data = new JObject
            {
                ["store"] = name,
                ["field"] = (string)parameters["field"]
            };
            routerClient.query("storeService.getValue", data, new JObject { }, callback);
        }

        public void addListener(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            var field = name + "." + (string)parameters["field"]; //TODO: add ability to add global listener
            routerClient.subscribe("storeService" + field, callback);
        }

        public void removeListener(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            var field = name + "." + (string)parameters["field"]; //TODO: add ability to add global listener
            routerClient.unsubscribe("storeService" + field, callback);
        }
    }
}
