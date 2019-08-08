using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ChartIQ.Finsemble
{
    /// <summary>
    /// The Window Client Manages Window State in the Workspace
    /// </summary>
    public class WindowClient
    {
        private Finsemble bridge;
        public JObject windowIdentifier { private set; get; }
        private StorageClient storageClient;
        private ConfigClient configClient;
        private string windowHash;
        private string containerHash;
        private const string WORKSPACE_CACHE_TOPIC = "finsemble.workspace.cache";
        private JObject options;

        internal WindowClient(Finsemble bridge)
        {
            this.bridge = bridge;
            this.storageClient = bridge.storageClient;
            this.windowHash = "activeWorkspace" + InternalHelper.TitleCase(bridge.windowName);
            this.containerHash = "activeWorkspace" + InternalHelper.TitleCase(bridge.windowName + " " + bridge.windowName);

            this.windowIdentifier = new JObject
            {
                ["windowName"] = bridge.windowName,
                ["uuid"] = bridge.uuid,
                ["componentType"] = bridge.componentType
            };

        }

        /// <summary>
        /// Given a field, this function retrieves app state.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="callback"></param>
        public void GetComponentState(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            var handler = (EventHandler<FinsembleEventArgs>) delegate (object sender, FinsembleEventArgs e)
            {
                JToken response = e.response["data"] as JToken;
                JToken responseData = new JObject { };
                if (e.error == null && response != null && response.HasValues) {
                    responseData = response[(string)parameters["field"]] as JToken;
                };
                callback(this, new FinsembleEventArgs(e.error, responseData));
            };
            storageClient.Get(new JObject { ["topic"] = WORKSPACE_CACHE_TOPIC, ["key"] = containerHash }, handler);
        }

        /// <summary>
        /// Given a field, this function sets and persists app state.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="callback"></param>
        public void SetComponentState(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            try
            {
                JArray fields;
                if (parameters["fields"] != null)
                {
                    fields = parameters["fields"] as JArray;
                }
                else
                {
                    fields = new JArray { };
                }
                if (parameters["field"] != null)
                {
                    fields.Add(new JObject
                    {
                        ["field"] = parameters["field"],
                        ["value"] = parameters["value"]
                    });
                }

                JObject storageValue = new JObject { };
                storageValue["state"] = new JObject { };
                storageValue["state"]["componentState"] = new JObject { };
                storageValue["windowName"] = bridge.windowName;

                foreach (var item in fields)
                {
                    storageValue["state"]["componentState"][(string)item["field"]] = item["value"];
                }

                bridge.RouterClient.Query("Finsemble.Workspace.SetWindowData", storageValue, new JObject { }, callback);
            } catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }


    }
}
