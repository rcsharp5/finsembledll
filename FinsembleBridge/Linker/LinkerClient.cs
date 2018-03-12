using System.Dynamic;
using ChartIQ.Finsemble;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Windows;
using System.Linq;

namespace ChartIQ.Finsemble
{
    public class LinkerClient
    {
        private FinsembleBridge bridge;
        private WindowClient windowClient;
        private RouterClient routerClient;
        private LauncherClient launcherClient;
        private StoreModel linkerStore;
        private JArray allChannels;
        private JArray channels;
        private JObject clients = new JObject { };
        private string key;
        private EventHandler<FinsembleEventArgs> stateChangeListeners;
        private Dictionary<string, EventHandler<FinsembleEventArgs>> linkerSubscribers = new Dictionary<string, EventHandler<FinsembleEventArgs>>();
        private List<string> channelListenerList = new List<string>();
        bool readyToPersistState = false;

        public LinkerClient(FinsembleBridge bridge)
        {
            this.bridge = bridge;
            windowClient = bridge.windowClient;
            routerClient = bridge.routerClient;
            launcherClient = bridge.launcherClient;
            key = (bridge.windowName + "::" + bridge.uuid).Replace('.', '_');

            var storehandler = (EventHandler<StoreModel>)delegate (object sender, StoreModel store)
            {
                linkerStore = store;
                linkerStore.GetValue(new JObject { ["field"] = "channels" }, delegate (object sender2, FinsembleEventArgs args)
                {
                    allChannels = args.response?["data"] as JArray;

                    var linkerStateHandler = (EventHandler<FinsembleEventArgs>)delegate (object sender3, FinsembleEventArgs args3)
                    {
                        //MessageBox.Show(args3?.response.ToString());
                        if (args3.response != null && args3.response.HasValues)
                        {
                            channels = args3.response as JArray;
                        } else
                        {
                            channels = new JArray { };
                        }

                        //MessageBox.Show(bridge.window, channels.ToString(), "", MessageBoxButton.YesNo);
                        //if (channels == null) 
                        var clientsInStore = new JObject { };
                        foreach (var item in channels)
                        {
                            clientsInStore[(string)item] = true;
                        }
                        clients[key] = new JObject
                        {
                            ["client"] = windowClient.windowIdentifier,
                            ["active"] = true,
                            ["channels"] = clientsInStore
                        };

                        readyToPersistState = true;
                        UpdateClientInStore(key);

                        stateChangeListeners?.Invoke(this, new FinsembleEventArgs
                        (
                            null, new JObject
                            {
                                ["channels"] = channels,
                                ["allChannels"] = allChannels
                            }
                        ));

                    };
                    bridge.windowClient.GetComponentState(new JObject { ["field"] = "Finsemble_Linker" }, linkerStateHandler);
                });

                linkerStore.AddListener(new JObject
                {
                    ["field"] = "clients." + key
                }, (EventHandler<FinsembleEventArgs>)delegate (object sender4, FinsembleEventArgs args4)
                {
                    var newChannelsObject = args4.response?["data"]?["value"]?["channels"] as JObject;
                    var newChannelsArray = new JArray { };
                    if (newChannelsObject != null)
                    {
                        foreach (var item in newChannelsObject)
                        {
                            newChannelsArray.Add(item.Key);
                        }
                    }
                    channels = newChannelsArray;

                    stateChangeListeners?.Invoke(this, new FinsembleEventArgs
                    (
                        null, new JObject
                        {
                            ["channels"] = channels,
                            ["allChannels"] = allChannels
                        }
                    ));

                    if (readyToPersistState)
                    {
                        PersistState();
                    }

                    UpdateListeners();
                });

                linkerStore.AddListener(null,
                (EventHandler<FinsembleEventArgs>)delegate (object sender4, FinsembleEventArgs args4)
                {
                    var newAllChannels = args4.response?["data"]?["value"]?["allChannels"] as JArray;
                    if (newAllChannels != null) allChannels = newAllChannels;
                    clients = args4.response?["data"]?["clients"] as JObject;
                    if (clients == null) clients = new JObject { };
                });

            };
            bridge.distributedStoreClient.GetStore(new JObject { ["store"] = "Finsemble_Linker", ["global"] = true }, storehandler);
        }

        private void PersistState()
        {
            try
            {
                windowClient.SetComponentState(new JObject
                {
                    ["field"] = "Finsemble_Linker",
                    ["value"] = channels
                }, (EventHandler<FinsembleEventArgs>)delegate (object s, FinsembleEventArgs e) { });
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private string MakeKey(JObject windowIdentifier)
        {
            return ((string)windowIdentifier["windowName"] + "::" + (string)windowIdentifier["uuid"]).Replace('.', '_');
        }

        private void handleListeners(object sender, FinsembleEventArgs args)
        {
            var topic = args.response["data"]?["type"].ToString();
            if (linkerSubscribers.ContainsKey(topic))
            {
                linkerSubscribers[topic]?.Invoke(this, new FinsembleEventArgs(null, new JObject
                {
                    ["data"] = args.response["data"]?["data"],
                    ["header"] = args.response["header"]
                }));
            }
        }

        private void UpdateListeners()
        {
            // Remove listeners
            for (var i = channelListenerList.Count - 1; i >= 0; i--)
            {
                var item = channelListenerList[i];
                if (channels.Where(jt => jt.Value<string>() == item.ToString()).Count() == 0)
                {
                    channelListenerList.RemoveAt(i);
                    routerClient.RemoveListener(item.ToString(), handleListeners);
                }
            }

            // Add new ones
            foreach (var item in channels)
            {
                if (!channelListenerList.Contains(item.ToString()))
                {
                    channelListenerList.Add(item.ToString());
                    routerClient.AddListener(item.ToString(), handleListeners);
                }
            }
        }

        private void UpdateClientInStore(string key)
        {
            linkerStore.SetValue(new JObject
            {
                ["field"] = "clients." + key,
                ["value"] = clients[key]
            }, (EventHandler<FinsembleEventArgs>)delegate (object sender, FinsembleEventArgs args) { });
        }

        public void linkToChannel(string channel, JObject windowIdentifier, EventHandler<FinsembleEventArgs> callback)
        {
            string keyToUse = key;
            if (windowIdentifier["windowName"] == null)
            {
                windowIdentifier = windowClient.windowIdentifier;
            }
            else
            {
                keyToUse = MakeKey(windowIdentifier);
            }

            if (clients[keyToUse] == null)
            {
                clients[keyToUse] = new JObject
                {
                    ["client"] = windowIdentifier,
                    ["channels"] = { }
                };
            }

            clients[keyToUse]["channels"][channel] = true;
            UpdateClientInStore(keyToUse);
        }

        public void UnlinkFromChannel(string channel, JObject windowIdentifier, EventHandler<FinsembleEventArgs> callback)
        {
            string keyToUse = key;
            if (windowIdentifier["windowName"] == null)
            {
                windowIdentifier = windowClient.windowIdentifier;
            }
            else
            {
                keyToUse = MakeKey(windowIdentifier);
            }

            clients[keyToUse]?["channels"]?[channel]?.Remove();
            UpdateClientInStore(keyToUse);
        }

        public void Publish(JObject parameters)
        {
            //bridge.SendRPCCommand(Topic.Publish, data);
            if (channels != null)
            {
                foreach (var item in channels)
                {
                    routerClient.Transmit((string)item + '.' + (string)parameters["dataType"], new JObject
                    {
                        ["type"] = (string)parameters["dataType"],
                        ["data"] = parameters["data"]
                    });
                    routerClient.Transmit((string)item, new JObject
                    {
                        ["type"] = (string)parameters["dataType"],
                        ["data"] = parameters["data"]
                    });
                }
            }
        }

        public void Subscribe(string channel, EventHandler<FinsembleEventArgs> callback)
        {
            if (linkerSubscribers.ContainsKey(channel))
            {
                linkerSubscribers[channel] += callback;
            }
            else
            {
                linkerSubscribers.Add(channel, callback);
            }
        }

        public void Unsubscribe(string channel, EventHandler<FinsembleEventArgs> callback)
        {
            linkerSubscribers[channel] -= callback;
        }

        public void ShowLinkerWindow()
        {

            var channelsToSend = new JArray { };
            if (channels != null)
            {
                foreach (var item in channels)
                {
                    var channelinfo = allChannels.Where(jt => jt["name"].ToString() == item.ToString())?.First();
                    if (channelinfo != null) channelsToSend.Add(channelinfo);
                }
            }
            JObject data = new JObject
            {
                ["channels"] = channelsToSend,
                ["windowIdentifier"] = windowClient.windowIdentifier
            };

            routerClient.Query("Finsemble.LinkerWindow.SetActiveChannels", data, new JObject { }, (EventHandler<FinsembleEventArgs>)delegate (object sender, FinsembleEventArgs e)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate //main thread
                {

                    var wi = new JObject
                    {
                        ["componentType"] = "linkerWindow"
                    };
                    var parameters = new JObject
                    {
                        ["position"] = "relative",
                        ["left"] = 0,
                        ["top"] = 30,
                        ["spawnIfNotFound"] = false
                    };
                    launcherClient.ShowWindow(wi, parameters, (EventHandler<FinsembleEventArgs>)delegate (object s2, FinsembleEventArgs e2) { });

                });
            });
        }

        public JArray GetAllChannels(EventHandler<FinsembleEventArgs> callback)
        {
            var args = new FinsembleEventArgs(null, JObject.Parse(JsonConvert.SerializeObject(allChannels)));
            callback(this, args);
            return this.allChannels;
        }

        public void OnStateChange(EventHandler<FinsembleEventArgs> callback)
        {
            stateChangeListeners += callback;
        }

    }
}
