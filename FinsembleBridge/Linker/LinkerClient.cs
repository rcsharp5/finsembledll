using System.Dynamic;
using ChartIQ.Finsemble;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Windows;

namespace ChartIQ.Finsemble
{
    public class LinkerClient
    {
        private FinsembleBridge bridge;
        private WindowClient windowClient;
        private RouterClient routerClient;
        private LauncherClient launcherClient;
        private StoreModel linkerStore;
        private List<LinkerChannel> allChannels;
        private List<LinkerChannel> channels;
        private JObject clients;


        public LinkerClient(FinsembleBridge bridge)
        {
            this.bridge = bridge;
            windowClient = bridge.windowClient;
            routerClient = bridge.routerClient;
            launcherClient = bridge.launcherClient;

            var storehandler = (EventHandler<StoreModel>)delegate (object sender, StoreModel store)
            {
                linkerStore = store;
            };
            bridge.distributedStoreClient.getStore(new JObject { ["store"] = "Finsemble_Linker", ["global"] = true }, storehandler);
        }

        public void linkToChannel(string groupName)
        {
            //bridge.SendRPCCommand(Topic.AddToGroup, groupName);
        }

        public void unlinkFromChannel(string groupName)
        {
            //bridge.SendRPCCommand(Topic.RemoveFromGroup, groupName);
        }

        public void publish(JObject data)
        {
            //bridge.SendRPCCommand(Topic.Publish, data);
        }

        public void subscribe(string channel)
        {
            //bridge.SendRPCCommand(Topic.Subscribe, "symbol", bridge.CallbackChannel.Subscribe);
        }

        public void unsubscribe(string channel)
        {
            //TODO
        }

        public void showLinkerWindow()
        {
            JObject data = new JObject
            {
                ["channels"] = new JArray { },
                ["windowIdentifier"] = windowClient.windowIdentifier
            };

            routerClient.query("Finsemble.LinkerWindow.SetActiveChannels", data, new JObject { }, (EventHandler<FinsembleEventArgs>)delegate (object sender, FinsembleEventArgs e)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate //main thread
                {

                    var wi = new JObject
                    {
                        ["componentType"] = "linkerWindow"
                    };
                    var parameters = new JObject
                    {
                        ["left"] = bridge.window.Left,
                        ["top"] = bridge.window.Top + 30,
                        ["spawnIfNotFound"] = false
                    };
                    launcherClient.showWindow(wi, parameters, (EventHandler<FinsembleEventArgs>)delegate (object s2, FinsembleEventArgs e2) { });

                });
            });
        }

        public List<LinkerChannel> getAllChannels(EventHandler<FinsembleEventArgs> callback)
        {
            var args = new FinsembleEventArgs(null, JObject.Parse(JsonConvert.SerializeObject(allChannels)));
            callback(this, args);
            return this.allChannels;
        }

    }
}
