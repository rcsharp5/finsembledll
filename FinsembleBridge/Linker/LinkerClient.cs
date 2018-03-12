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
    /// <summary>
    /// <para>The Linker API provides a mechanism for synchronizing components on a piece of data. For instance, a user might link multiple components by "stock symbol". Using the Linker API, a developer could enable their component to participate in this synchronization. The developer would use {@link LinkerClient#subscribe} to receive synchronization events and they would use {@link LinkerClient#publish} to send them. The Linker API is inherently similar to The {@link RouterClient} pub/sub mechanism. The primary difference is that the Linker API is designed for end-user interaction. By exposing the Linker API, developers allow *end users* to create and destroy linkages at run-time.</para>
    /// <para>In order for components to be linked, they must understand the data format that will be passed betweenthem (the "context"), and agree on a label to identifies that format (the "dataType"). For instance, components might choose to publish and subscribe to a dataType called "symbol". They would then also need to agree that a "symbol" looks like. The Linker API doesn't proscribe any specific format for context or set of labels (some would call this a "taxonomy"). See OpenFin's FDC3 project for an emerging industry standard taxonomy.</para>
    /// <para>End users create linkages by assigning components to "channels". Our default implementation represents channels by color. When a component is assigned to channel "purple", publish and subscribe messages are only received by other components assigned to that channel. If you're using Finsemble's built in Linker component, you won't have to code this. The Linker component does the work of assigning and unassigning its associated component to the selected channel. However, the Linker API exposes functionality so that you can manage channels programatically if you choose. You could use these functions to build your own Linker Component using a different paradigm, or apply intelligently link components based on your own business logic. *Note, it is not necessary to stick to a color convention. Channels are simple strings and so can be anything.*</para>
    /// <para>Behind the scenes, the Linker Service coordinates Linker activity between components. It keeps track of the available channels and channel assignments. It uses a dedicated distributed store to maintain this information and also persists the information to workspaces.</para>
    /// </summary>
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

        internal LinkerClient(FinsembleBridge bridge)
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

        /// <summary>
        /// Add a component to a Linker channel programatically. Components will begin receiving any new contexts published to this channel but will not receive the currently established context.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="windowIdentifier">If windowIdentifier is null, it uses current window.</param>
        /// <param name="callback"></param>
        public void linkToChannel(string channel, JObject windowIdentifier, EventHandler<FinsembleEventArgs> callback)
        {
            string keyToUse = key;
            if (windowIdentifier == null)
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

        /// <summary>
        /// Unlinks a component from a Linker channel.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="windowIdentifier">If windowIdentifier is null, it uses current window.</param>
        /// <param name="callback"></param>
        public void UnlinkFromChannel(string channel, JObject windowIdentifier, EventHandler<FinsembleEventArgs> callback)
        {
            string keyToUse = key;
            if (windowIdentifier == null)
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

        /// <summary>
        /// Publish a piece of data. The data will be published to all channels that the component is linked to. Foreign components that are linked to those channels will receive the data if they have subscribed to this dataType. They can then use that data to synchronize their internal state. See Subscribe
        /// </summary>
        /// <param name="parameters">parameters["dataType"] is a string that represents the data type. paramters["data"] is a JObject/JArray representing the data.</param>
        public void Publish(JObject parameters)
        {
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

        /// <summary>
        /// Registers a client for a specific data type that is sent to a group.
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="callback">A function to be called once the linker receives the specific data.</param>
        public void Subscribe(string dataType, EventHandler<FinsembleEventArgs> callback)
        {
            if (linkerSubscribers.ContainsKey(dataType))
            {
                linkerSubscribers[dataType] += callback;
            }
            else
            {
                linkerSubscribers.Add(dataType, callback);
            }
        }

        /// <summary>
        /// Remove a listener for a specific dataType
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="callback"></param>
        public void Unsubscribe(string dataType, EventHandler<FinsembleEventArgs> callback)
        {
            linkerSubscribers[dataType] -= callback;
        }

        /// <summary>
        /// Shows the Finsemble Linker Window
        /// </summary>
        /// <param name="left">Left of Linker Window relative to the current window (default 0)</param>
        /// <param name="top">Top of Linker Window relative to the current window (default 30)</param>
        public void ShowLinkerWindow(double left = 0, double top = 30)
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
                        ["left"] = left,
                        ["top"] = top,
                        ["spawnIfNotFound"] = false
                    };
                    launcherClient.ShowWindow(wi, parameters, (EventHandler<FinsembleEventArgs>)delegate (object s2, FinsembleEventArgs e2) { });

                });
            });
        }

        /// <summary>
        /// Returns all available Linker channels
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public JArray GetAllChannels(EventHandler<FinsembleEventArgs> callback)
        {
            var args = new FinsembleEventArgs(null, JObject.Parse(JsonConvert.SerializeObject(allChannels)));
            callback(this, args);
            return this.allChannels;
        }

        /// <summary>
        /// Use this method to register a callback which will be called whenever the state of the Linker changes. This will be called whenever a user links or unlinks your component to a channel.
        /// </summary>
        /// <param name="callback"></param>
        public void OnStateChange(EventHandler<FinsembleEventArgs> callback)
        {
            stateChangeListeners += callback;
        }

    }
}
