using System.Dynamic;
using ChartIQ.Finsemble;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Windows;
using System.Linq;
using System.Timers;

namespace ChartIQ.Finsemble
{
    /// <summary>
    /// <para>The Linker API provides a mechanism for synchronizing components on a piece of data. For instance, a user might link multiple components by "stock symbol". Using the Linker API, a developer could enable their component to participate in this synchronization. The developer would use {@link LinkerClient#subscribe} to receive synchronization events and they would use {@link LinkerClient#publish} to send them. The Linker API is inherently similar to The {@link RouterClient} pub/sub mechanism. The primary difference is that the Linker API is designed for end-user interaction. By exposing the Linker API, developers allow *end users* to create and destroy linkages at run-time.</para>
    /// <para>In order for components to be linked, they must understand the data format that will be passed betweenthem (the "context"), and agree on a label to identifies that format (the "dataType"). For instance, components might choose to publish and subscribe to a dataType called "symbol". They would then also need to agree that a "symbol" looks like. The Linker API doesn't proscribe any specific format for context or set of labels (some would call this a "taxonomy"). See OpenFin's FDC3 project for an emerging industry standard taxonomy.</para>
    /// <para>End users create linkages by assigning components to "channels". Our default implementation represents channels by color. When a component is assigned to channel "purple", publish and subscribe messages are only received by other components assigned to that channel. If you're using Finsemble's built in Linker component, you won't have to code this. The Linker component does the work of assigning and unassigning its associated component to the selected channel. However, the Linker API exposes functionality so that you can manage channels programatically if you choose. You could use these functions to build your own Linker Component using a different paradigm, or apply intelligently link components based on your own business logic. *Note, it is not necessary to stick to a color convention. Channels are simple strings and so can be anything.*</para>
    /// <para>Behind the scenes, the Linker Service coordinates Linker activity between components. It keeps track of the available channels and channel assignments. It uses a dedicated distributed store to maintain this information and also persists the information to workspaces.</para>
    /// </summary>
    public class Share
    {
        private Finsemble bridge;

        //JObject dataReceivers;
        public Dictionary<string, EventHandler<FinsembleEventArgs>> dataReceivers { private set; get; } = new Dictionary<string, EventHandler<FinsembleEventArgs>>();

        JArray shareChannels;

        private WindowClient windowClient;
        private RouterClient routerClient;
        private LauncherClient launcherClient;
        private JObject clients = new JObject { };
        private string key;
        private EventHandler<FinsembleEventArgs> stateChangeListeners;
        private Dictionary<string, EventHandler<FinsembleEventArgs>> linkerSubscribers = new Dictionary<string, EventHandler<FinsembleEventArgs>>();
        private List<string> channelListenerList = new List<string>();
        private bool canReceiveData  =true;
        bool readyToPersistState = false;
        //private bool _useExplicitChannels = false;
        public Share(Finsemble bridge)
        {
            this.bridge = bridge;
            this.shareChannels = new JArray { };
            windowClient = bridge.WindowClient;
            routerClient = bridge.RouterClient;
            launcherClient = bridge.LauncherClient;
            key = (bridge.windowName + "::" + bridge.uuid).Replace('.', '_');

            addListeners();
        }



        /// <summary>
        /// subscribe to a dataType.
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="handler"></param>
        public void subscribe(string dataType, EventHandler<FinsembleEventArgs> handler)
        {
            JObject parameters = new JObject { };
            parameters["windowName"] = this.bridge.windowName;
            parameters["dataType"] = dataType;
            routerClient.Query("share.subscribe", parameters, (object sender, FinsembleEventArgs e) =>
            {
                if (!dataReceivers.ContainsKey(dataType))
                {
                    dataReceivers.Add(dataType, handler);
                }
            });
        }
        /// <summary>
        /// unsubscribe to a dataType.
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="handler"></param>
        public void unSubscribe(string dataType)
        {
            JObject parameters = new JObject { };
            parameters["windowName"] = this.bridge.windowName;
            parameters["dataType"] = dataType;
            routerClient.Query("share.unSubscribe", parameters, (object sender, FinsembleEventArgs e) =>
            {
                if (!dataReceivers.ContainsKey(dataType));
                {
                    dataReceivers.Remove(dataType);
                }
            });
        }

        /// <summary>
        /// Link to a channel.
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="handler"></param>
        public void linkToChannel(string channel)
        {
            JObject parameters = new JObject { };
            parameters["windowName"] = this.bridge.windowName;
            parameters["channel"] = channel;

            routerClient.Query("share.linker.linkToChannel", parameters, (s, args) =>
            {
                JObject data =(JObject) args.response["data"];
                if (data !=null && data["channels"] != null) 
                {
                    this.shareChannels = (JArray)data["channels"];
                }

            });
        }
        /// <summary>
        /// Link to a channel.
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="handler"></param>
        public void linkToChannel(string channel, EventHandler<FinsembleEventArgs> callback)
        {
            JObject parameters = new JObject { };
            parameters["windowName"] = this.bridge.windowName;
            parameters["channel"] = channel;

            routerClient.Query("share.linker.linkToChannel", parameters, (s, args) =>
            {
                JObject data = (JObject)args.response["data"];
                if(data == null)
                {
                    callback(s, new FinsembleEventArgs(new JObject { },null));
                }
                if (data != null && data["channels"] != null)
                {
                    this.shareChannels = (JArray)data["channels"];
                    callback(s, new FinsembleEventArgs(null,null));
                }

            });
        }

        public void unLinkToChannel(string channel)
        {
            JObject parameters = new JObject { };
            parameters["windowName"] = this.bridge.windowName;
            parameters["channel"] = channel;

            routerClient.Query("share.linker.unLinkToChannel", parameters, (s, args) =>
            {
                JObject data = (JObject)args.response["data"];
                if (data != null && data["channels"] != null)
                {
                    this.shareChannels = (JArray)data["channels"];
                }
            });
        }
        public void unLinkToChannel(string channel, EventHandler<FinsembleEventArgs> callback)
        {
            JObject parameters = new JObject { };
            parameters["windowName"] = this.bridge.windowName;
            parameters["channel"] = channel;

            routerClient.Query("share.linker.unLinkToChannel", parameters, (s, args) =>
            {
                JObject data = (JObject)args.response["data"];
                 if (data == null)  callback(s,new FinsembleEventArgs(new JObject { }, null));
                if (data != null && data["channels"] != null)
                {
                    this.shareChannels = (JArray)data["channels"];
                    callback(s, new FinsembleEventArgs(null,null));
                }
            });
        }
        /// <summary>
        /// publish  to a channel.
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="handler"></param>
        public void publishToLink(JObject parms, EventHandler<FinsembleEventArgs> callback)
        {
            canReceiveData = false;
            JObject parameters = new JObject { };
            parameters["windowName"] = this.bridge.windowName;
            parameters["dataType"] = parms["dataType"];
            parameters["data"] = parms["data"];
           
            routerClient.Query("share.linker.publishToLink", parameters, (s,args)=> {

                Timer timer = new System.Timers.Timer();
                timer.Interval = 5000;

                timer.Elapsed += (Object source, System.Timers.ElapsedEventArgs e) => {
                    timer.Stop();
                    canReceiveData = true ;
                };
               
                timer.Enabled = true;
                timer.Start();
            });
        }


        /// <summary>
        /// publish  to a channel.
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="handler"></param>
        private void handleSharedData(object sender, FinsembleEventArgs e)
        {
            if (!canReceiveData) return;
            string dataType = (string) e.response["data"]["dataType"];
            if (dataType == null) return;

            foreach(var receiver in dataReceivers)
            {
                if (receiver.Key != dataType) continue;
               
                receiver.Value.Invoke(this, new FinsembleEventArgs(null, new JObject { ["data"] = e.response["data"] }));
            }
          
        }

        /// <summary>
        /// publish  to a channel.
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="handler"></param>
        private void addListeners()
        {
  
            routerClient.AddListener("share.onSharedData." + this.bridge.windowName, handleSharedData);
        }



    }
}
