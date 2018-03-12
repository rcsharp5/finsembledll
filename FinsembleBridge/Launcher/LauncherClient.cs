using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ChartIQ.Finsemble
{
    /// <summary>
    /// The Launcher client handles spawning windows and window groups.
    /// </summary>
    public class LauncherClient
    {
        private FinsembleBridge bridge;
        private RouterClient routerClient;
        private WindowClient windowClient;
        public EventHandler<FinsembleEventArgs> windowGroupUpdateHandler;

        internal LauncherClient(FinsembleBridge bridge)
        {
            this.bridge = bridge;
            routerClient = bridge.routerClient;
            windowClient = bridge.windowClient;

            // Heartbeat
            var timer = new Timer();
            timer.Interval = 1000;
            timer.Elapsed += (sender, e) => {
                routerClient.Transmit("Finsemble.heartbeat", new JObject
                {
                    ["type"] = "component",
                    ["componentType"] = "finsemble",
                    ["windowName"] = bridge.windowName
                });
            };
            timer.Enabled = true;

            // Window Groups
            windowClient.GetComponentState(new JObject
            {
                ["field"] = "finsemble:windowGroups"
            }, (err, groups) => {
                if (groups.response != null)
                {
                    AddToGroups(new JObject
                    {
                        ["groupNames"] = groups.response
                    }, SubscribeToGroupUpdates);
                }
            });
        }

        private void SubscribeToGroupUpdates(object sender, FinsembleEventArgs e)
        {
            routerClient.Subscribe("Finsemble.LauncherService.updateGroups." + bridge.windowName, (s, fe) =>
            {
                windowClient.SetComponentState(new JObject
                {
                    ["field"] = "finsemble:windowGroups",
                    ["value"] = fe.response["data"]
                }, (s2, e2) => {
                    
                });
                windowGroupUpdateHandler?.Invoke(sender, new FinsembleEventArgs(e.error, e.response?["data"]));
            });
        }

        /// <summary>
        /// Show A Finsemble Component.
        /// </summary>
        /// <param name="windowIdentifier">A JObject containing a windowName, uuid and componentType</param>
        /// <param name="parameters"></param>
        /// <param name="callback"></param>
        public void ShowWindow(JObject windowIdentifier, JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            parameters["windowIdentifier"] = windowIdentifier;
            parameters["relativeWindow"] = windowClient.windowIdentifier;
            routerClient.Query("Launcher.showWindow", parameters, new JObject { }, callback);
        }

        /// <summary>
        /// Spawn a Finsemble Window.
        /// </summary>
        /// <param name="component">componentType of the component to spwan</param>
        /// <param name="parameters"></param>
        /// <param name="callback"></param>
        public void Spawn(string component, JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            parameters["launchingWindow"] = windowClient.windowIdentifier;
            parameters["component"] = component;
            routerClient.Query("Launcher.spawn", parameters, new JObject { }, callback);
        }

        /// <summary>
        /// Gets window groups for current window
        /// </summary>
        /// <param name="callback"></param>
        public void GetGroupsForWindow(EventHandler<FinsembleEventArgs> callback)
        {
            windowClient.GetComponentState(new JObject
            {
                ["field"] = "finsemble:windowGroups"
            }, callback);
        }

        /// <summary>
        /// Gets window groups for any window. Specify parameters["windowIdentifier"] as the windowIdentifier for the window that you want groups for
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="callback"></param>
        public void GetGroupsForWindow(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            routerClient.Query("LauncherService.getGroupsForWindow", parameters, new JObject { }, (sender, args) => {
                callback(sender, new FinsembleEventArgs(args.error, args.response["data"]));
            });
        }

        /// <summary>
        /// Adds window to groups
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="callback"></param>
        public void AddToGroups(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            if(parameters["windowIdentifier"] == null)
            {
                parameters["windowIdentifier"] = windowClient.windowIdentifier;
            }
            routerClient.Query("LauncherService.addWindowToGroups", parameters, new JObject { }, callback);
        }
        
    }
}
