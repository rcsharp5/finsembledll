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

        /// <summary>
        /// Sets a value in a distributed store.
        /// </summary>
        /// <example>
        /// <code>
        ///     store.SetValue(new JObject{["field"] = "field1", ["value"] = "new value"}, (sender, args) => {
        ///         // value was set in store. Nothing needs to be done here.
        ///     });
        /// </code>
        /// </example>
        /// <param name="parameters"></param>
        /// <param name="callback"></param>
        public void SetValue(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            var data = new JObject
            {
                ["store"] = name,
                ["field"] = (string)parameters["field"],
                ["value"] = parameters["value"]
            };
            routerClient.Query("storeService.setValue", data, new JObject { }, callback);
        }

        /// <summary>
        /// Get a value from the distributed store
        /// </summary>
        /// <example>
        /// <code>
        ///     store.GetValue(new JObject {["field"] = "field1"}, (sender, args) => {
        ///         var fieldValue = args.response;
        ///     })
        /// </code>
        /// </example>
        /// <param name="parameters"></param>
        /// <param name="callback"></param>
        public void GetValue(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            var data = new JObject
            {
                ["store"] = name,
                ["field"] = (string)parameters["field"]
            };
            routerClient.Query("storeService.getValue", data, new JObject { }, callback);
        }

        /// <summary>
        /// Add a listener that fires when a specific value in a store is changed.
        /// </summary>
        /// <example>
        /// <code>
        ///     store.AddListener(new JObject {["field"] = "field1"}, myHandler);
        ///     store.AddListener(new JObject {["field"] = "field1"}, (sender, args) => {
        ///         var valueOfField = args.response?["data"]?["value"]
        ///     });
        /// </code>
        /// </example>
        /// <param name="parameters"></param>
        /// <param name="callback"></param>
        public void AddListener(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            string field;
            if (parameters != null)
            {
                field = name + "." + (string)parameters["field"]; //TODO: add ability to add global listener
            } else
            {
                field = "";
            }
            routerClient.Subscribe("storeService" + field, callback);
        }

        /// <summary>
        /// Add a listener that fires when a specific value in a store is changed.
        /// </summary>
        /// <example>
        /// <code>
        ///     store.RemoveListener(new JObject {["field"] = "field1"}, myHandler);
        /// </code>
        /// </example>
        /// <param name="parameters"></param>
        /// <param name="callback"></param>
        public void RemoveListener(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            string field;
            if (parameters != null)
            {
                field = name + "." + (string)parameters["field"]; //TODO: add ability to add global listener
            } else
            {
                field = "";
            }
            routerClient.Unsubscribe("storeService" + field, callback);
        }
    }
}
