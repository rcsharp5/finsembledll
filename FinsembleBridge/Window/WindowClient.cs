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
        private string containerHash;
        private const string WORKSPACE_CACHE_TOPIC = "finsemble.workspace.cache";

        public WindowClient(FinsembleBridge bridge)
        {
            this.bridge = bridge;
            this.storageClient = bridge.storageClient;
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
                JObject response = e.response;
                if (e.error == null) {
                    response = e.response[parameters["field"]] as JObject;
                };
                callback(this, new FinsembleEventArgs(e.error, response));
            };
            storageClient.get(new JObject { ["topic"] = WORKSPACE_CACHE_TOPIC, ["key"] = containerHash }, handler);
        }

    }
}
