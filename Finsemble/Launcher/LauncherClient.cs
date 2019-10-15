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
    public class LauncherClient : IDisposable
    {
		private readonly Timer timer = new Timer();

		private Finsemble bridge;
        private RouterClient routerClient;
        private WindowClient windowClient;
        public EventHandler<FinsembleEventArgs> windowGroupUpdateHandler;

        internal LauncherClient(Finsemble bridge)
        {
            this.bridge = bridge;
            routerClient = bridge.RouterClient;
            windowClient = bridge.WindowClient;
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

            // Heartbeat
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

        public void GetWindowsInGroup(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            routerClient.Query("LauncherService.getWindowsInGroup", new JObject { ["groupName"] = parameters["groupName"] }, new JObject { }, (sender, args) =>
            {
                callback(sender, new FinsembleEventArgs(args.error, args.response?["data"]));
            });
        }

        public void HyperFocus(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            if (parameters["windowList"] == null && parameters["groupName"] == null && parameters["componentType"] == null) {
                parameters["windowList"] = new JArray();
                (parameters["windowList"] as JArray).Add(windowClient.windowIdentifier);
            }
            routerClient.Transmit("LauncherService.hyperFocus", parameters);
            callback(this, new FinsembleEventArgs(null, null));
        }

        public void BringWindowsToFront(JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            if (parameters["windowList"] == null && parameters["groupName"] == null && parameters["componentType"] == null)
            {
                parameters["windowList"] = new JArray();
                (parameters["windowList"] as JArray).Add(windowClient.windowIdentifier);
            }
            routerClient.Transmit("LauncherService.bringWindowsToFront", parameters);
            callback(this, new FinsembleEventArgs(null, null));
        }

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
					timer.Stop();
					timer.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~LauncherClient() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}
