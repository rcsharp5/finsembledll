using System;
using System.Diagnostics;
using System.Reflection;
using log4net;
using Newtonsoft.Json.Linq;
using Openfin.Desktop;

namespace ChartIQ.Finsemble
{
    public partial class FinsembleBridge : IDisposable
    {
        /// <summary>
        /// The logger
        /// </summary>
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Connection parameters
        /// <summary>
        /// The version of the OpenFin runtime that will be requested by this example.
        /// </summary>
        /// <remarks>
        /// Version must match the version of OpenFin used by Finsemble for inter-application communication to be
        /// possible.
        /// </remarks>
        public Version OpenFinVersion { get; private set; }
        #endregion

        /// <summary>
        /// The instance of the OpenFin used by this example.
        /// </summary>
        public Runtime runtime {get; private set;}

        #region Instance constants
        /// <summary>
        /// Class containing the callback channel names unique to this instance of the application.
        /// </summary>
        public readonly CallbackChannel CallbackChannel = new CallbackChannel();
        #endregion

        #region Events
        /// <summary>
        /// Event that occurs when bridge successfully connects to the OpenFin inter-application bus.
        /// </summary>
        public event EventHandler Connected;

        /// <summary>
        /// Event that occurs when the bridge is disconnected from the OpenFin inter-application bus.
        /// </summary>
        public event EventHandler Disconnected;

        /// <summary>
        /// Event that occurs when there is an error with the bridge.
        /// </summary>
        public event EventHandler<UnhandledExceptionEventArgs> Error;

        /// <summary>
        /// Event that occurs when a linker subscription message comes in.
        /// </summary>
        public event EventHandler<LinkerEventArgs> LinkerSubscribe;
        #endregion

        public string componentType { private set; get; }
        public string windowName { private set; get; }
        public string uuid { private set; get; }

        public RouterClient routerClient { private set; get; }
        public DistributedStoreClient distributedStoreClient { private set; get; }
        public StorageClient storageClient { private set; get; }
        public WindowClient windowClient { private set; get; }
        public LauncherClient launcherClient { private set; get; }
        public LinkerClient linkerClient { private set; get; }
        public ConfigClient configClient { private set; get; }
        public System.Windows.Window window { private set; get; }
        public Docking docking;

        /// <summary>
        /// Initializes a new instance of the FinsembleBridge class.
        /// </summary>
        /// <param name="openFinVersion">The version of the OpenFin runtime to which to connect</param>
        public FinsembleBridge(Version openFinVersion, string windowName, string componentType, System.Windows.Window window)
        {
            Logger.Debug(
                "Initializing new instance of FinsembleBridge:\n" +
                $"\tVersion: {openFinVersion}\n");

            OpenFinVersion = openFinVersion;
            this.windowName = windowName;
            this.componentType = componentType;
            this.window = window;
        }

        /// <summary>
        /// Connect to the OpenFin inter-application bus.
        /// </summary>
        public void Connect()
        {
            Logger.Debug("Connect called");
            if (runtime != null)
            {
                // Already connected
                return;
            }

            var runtimeOptions = new RuntimeOptions
            {
                Version = OpenFinVersion.ToString(),
            };

            runtime = Runtime.GetRuntimeInstance(runtimeOptions);

            // Set up error handler.
            runtime.Error += (s, e) =>
            {
                try
                {
                    Exception ex = (Exception)e.ExceptionObject;
                    Logger.Error("Error from OpenFin runtime", ex);
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        $"Error from OpenFin runtime not an exception:\n {e.ExceptionObject.ToString()}",
                        ex);
                }

                // Notify listeners there was an error
                Error?.Invoke(this, e);
            };

            // Set up disconnected handler
            runtime.Disconnected += (s, e) =>
            {
                Logger.Info("Disconnected from OpenFin runtime.");

                // Notify listeners bridge is disconnected from OpenFin
                Disconnected?.Invoke(this, e);
            };

            // Connect to the OpenFin runtime.
            runtime.Connect(() =>
            {
                Logger.Info("Connected to OpenFin Runtime.");

                // Listen for the various linker method callbacks
                ListenForCallbacks();

                this.uuid = runtime.Options.UUID;

                routerClient = new RouterClient(this);
                storageClient = new StorageClient(this);
                configClient = new ConfigClient(this); 
                windowClient = new WindowClient(this); 
                launcherClient = new LauncherClient(this); 
                distributedStoreClient = new DistributedStoreClient(this);
                linkerClient = new LinkerClient(this);

                docking = new Docking(this, window, windowName, windowName + "-channel");

                // Notify listeners that connection is complete.
                // ToDo, wait for clients to be ready??
                Connected?.Invoke(this, EventArgs.Empty);
            });
        }

        /// <summary>
        /// Disconnects from OpenFin inter-application bus.
        /// </summary>
        public void Disconnect()
        {
            Logger.Info("Disconnect called");
            if (runtime == null)
            {
                // Already disconnected
                return;
            }

            runtime.Disconnect(() => { });
        }

        /// <summary>
        /// Sends an RPC command to Finsemble.
        /// </summary>
        /// <param name="parameters">The parameters being publish to the inter-application bus for Finsemble.</param>
        /// <example>
        /// // Create a parameters object specifying the data type and data being published.
        /// JObject parameters = new JObject
        /// {
        ///		["dataType"] = "symbol",
        ///		["data"] = "AAPL"
        /// };
        /// 
        /// // Send the parameters to the publish channel for the RPCService to send out to the rest of Finsemble.
        /// SendRPCCommand("FSBL.Clients.LinkerClient.publish", parameters);
        /// </example>
        public void SendRPCCommand(string topic, JObject parameters)
        {
            Logger.Debug($"Sending RPC command: topic: \"{topic}\", parameters: {parameters.ToString()}");
            JArray args = new JArray
            {
                parameters
            };

            JObject message = new JObject
            {
                ["args"] = args
            };

            runtime.InterApplicationBus.publish(topic, message);
        }

        /// <summary>
        /// Sends an RPC command to Finsemble.
        /// </summary>
        /// <param name="topic">The topic under which the parameter will be published.</param>
        /// <param name="parameter">The parameter being publish to the inter-application bus for Finsemble.</param>
        /// <param name="callbackChannel">Optional callback channel used for being notified of changes.</param>
        /// <example>
        /// // Add application to Group 1
        /// SendRPCCommand("FSBL.Clients.LinkerClient.addToGroup", "group1");
        /// 
        /// // Remove application from Group 1
        /// SendRPCCommand("FSBL.Clients.LinkerClient.removeFromGroup", "group1");
        /// 
        /// // Send a subscribe to symbol data type message.
        /// SendRPCCommand("FSBL.Clients.LinkerClient.subscribe", "symbol", "uniqueSubscribeCallbackChannel");
        /// </example>
        public void SendRPCCommand(string topic, string parameter, string callbackChannel = "")
        {
            Logger.Debug(
                "Sending RPC command:\n" +
                $"\ttopic: \"{topic}\"\n" +
                $"\tparameter: \"{parameter}\"\n" +
                $"\tcallbackChannel: {callbackChannel}");

            JArray args = new JArray
            {
                parameter
            };

            JObject message = new JObject
            {
                ["args"] = args
            };

            if (!string.IsNullOrWhiteSpace(callbackChannel))
            {
                Logger.Debug($"Adding callback channel: {callbackChannel}");
                message.Add("callbackChannel", callbackChannel);
            }

            runtime.InterApplicationBus.publish(topic, message);
        }

        public void SubscribeToChannel(string channel, Openfin.Desktop.InterAppMessageHandler callback)
        {
            runtime.InterApplicationBus.subscribe("*", channel, callback);
        }

        /// <summary>
        /// Subscribes to the inter-application bus to listen for Linker method callbacks. 
        /// </summary>
        /// <remarks>
        /// This method sets up a subscriptions on the inter-application bus listening linker for each of it's API 
		/// endpoints. When a change in the symbol occurs in a group to which the native application is subscribed, 
		/// Finsemble sends a message on the SubscribeCallbackChannel with new symbol. 
        /// </remarks>
        private void ListenForCallbacks()
        {
			Logger.Info("Listening for callbacks");

            Logger.Debug($".subscribe(\"*\", {CallbackChannel.Subscribe}, FireLinkerSubscribe)");

            // When data is sent back from Finsemble (e.g., a chart changes a symbol, and publishes on the green 
            // group), we will invoke the FireLinkerSubscribe.
            runtime.InterApplicationBus.subscribe("*", CallbackChannel.Subscribe, FireLinkerSubscribe);

            // TODO: Subscribe and respond to other linker method calls. 
        }

        /// <summary>
        /// Fires the LinkerSubscribe event.
        /// </summary>
        /// <param name="sourceUuid">The UUID of the source application</param>
        /// <param name="topic">The topic associated with the message</param>
        /// <param name="message">The message</param>
        private void FireLinkerSubscribe(string sourceUuid, string topic, object message)
        {
            Logger.Debug($"FireLinkerSubscribe({sourceUuid}, {topic}, {message.ToString()}");

            var h = LinkerSubscribe;
            if (h != null)
            {
                var jObj = message as JObject;
                var messageStr = jObj.GetValue("0").Value<string>();
                Debug.WriteLine($"Received message: {messageStr}");

                var e = new LinkerEventArgs(sourceUuid, topic, messageStr);
                h(this, e);
            }
        }

        public string CamelCase(string str)
        {
            var split = str.Split(' ');
            for(var i = 0; i<split.Length; i++)
            {
                split[i] = split[i].ToLower();
                if(!String.IsNullOrEmpty(split[1]))
                {
                    split[i] = char.ToUpper(split[i][0]) + split[i].Substring(1);
                }
            }
            return String.Join("", split);
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
                }

                Disconnect();

                runtime = null;

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FinsembleBridge() {
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
