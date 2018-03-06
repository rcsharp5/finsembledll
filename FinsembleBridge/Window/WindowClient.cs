using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartIQ.Finsemble
{
    public class WindowClient
    {
        private FinsembleBridge bridge;
        public JObject windowIdentifier { private set; get; }
        private StorageClient storageClient;
        private ConfigClient configClient;
        private string windowHash;
        private string containerHash;
        private const string WORKSPACE_CACHE_TOPIC = "finsemble.workspace.cache";
        private JObject options;

        public WindowClient(FinsembleBridge bridge)
        {
            this.bridge = bridge;
            this.storageClient = bridge.storageClient;
            this.windowHash = bridge.CamelCase("activeWorkspace " + bridge.windowName);
            this.containerHash = bridge.CamelCase("activeWorkspace " + bridge.windowName + " " + bridge.windowName);

            this.windowIdentifier = new JObject
            {
                ["windowName"] = bridge.windowName,
                ["uuid"] = bridge.uuid,
                ["componentType"] = bridge.componentType
            };

        }

        public void getComponentState(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            var handler = (EventHandler<FinsembleEventArgs>) delegate (object sender, FinsembleEventArgs e)
            {
                JObject response = e.response["data"] as JObject;
                if (e.error == null && response != null) {
                    response = response[parameters["field"]] as JObject;
                };
                callback(this, new FinsembleEventArgs(e.error, response));
            };
            storageClient.get(new JObject { ["topic"] = WORKSPACE_CACHE_TOPIC, ["key"] = containerHash }, handler);
        }

    }
}
