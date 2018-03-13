using System;
using System.Diagnostics;
using System.Reflection;
using log4net;
using Newtonsoft.Json.Linq;
using Openfin.Desktop;
using System.Collections.Generic;

namespace ChartIQ.Finsemble
{
    /// <summary>
    /// This is the Main Class used to access Finsemble in a .NET Application. Once connected, several Finsemble APIs can be used using the SendCommand method.
    /// <example>
    /// Connecting to Finsemble:
    /// <code>
    /// /*windowName, componentType and uuid are provided as command line parameters when the application is launched by Finsemble*/
    /// var finsemble = new FinsembleBridge(openFinVersion, windowName, componentType, window, uuid);
    /// finsemble.Connect += Finsemble_Connected;
    /// </code>
    /// 
    /// Using the Clients once connected:
    /// <code>
    /// private void Finsemble_Connected(object sender, EventArgs e) {
    ///     finsemble.SendCommand("LinkerClient.publish", new JObject {
    ///         ["dataType"] = "symbol",
    ///         ["data"] = "AAPL"
    ///     });
    /// }
    /// </code>
    /// </example>
    /// </summary>
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
        internal Runtime runtime {get; private set;}

        #region Instance constants
        /// <summary>
        /// Class containing the callback channel names unique to this instance of the application.
        /// </summary>
        internal readonly CallbackChannel CallbackChannel = new CallbackChannel();
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

        #endregion

        public string componentType { private set; get; }
        public string windowName { private set; get; }
        public string uuid { private set; get; }

        internal RouterClient routerClient { private set; get; }
        internal DistributedStoreClient distributedStoreClient { private set; get; }
        internal StorageClient storageClient { private set; get; }
        internal WindowClient windowClient { private set; get; }
        internal LauncherClient launcherClient { private set; get; }
        internal LinkerClient linkerClient { private set; get; }
        internal ConfigClient configClient { private set; get; }
        public AuthenticationClient authenticationClient { private set; get; }
        public System.Windows.Window window { private set; get; }
        internal Docking docking;
        private Dictionary<string, List<string>> dependencies = new Dictionary<string, List<string>>()
        {
            {"distributedStoreClient", new List<string>() {"dataStoreService"}  },
            {"launcherClient", new List<string> {"launcherService"} },
            {"linkerClient", new List<string> {"linkerService"} },
            {"windowClient", new List<string> {"storageService"} }
        };

        /// <summary>
        /// Initializes a new instance of the FinsembleBridge class. This is how you interact with Finsemble. All the clients will be part of the bridge.
        /// </summary>
        /// <param name="openFinVersion">The version of the OpenFin runtime to which to connect</param>
        /// <param name="windowName">The windowName parameter passed as a command line parameter when launched from Finsemble.</param>
        /// <param name="componentType">The componentType parameter passed as a command line parameter when launched from Finsemble.</param>
        /// <param name="window">The window that will be Finsembleized</param>
        /// <param name="uuid">The uuid parameter passed as a command line parameter when launched from Finsemble.</param>
        public FinsembleBridge(Version openFinVersion, string windowName, string componentType, System.Windows.Window window, string uuid)
        {
            Logger.Debug(
                "Initializing new instance of FinsembleBridge:\n" +
                $"\tVersion: {openFinVersion}\n");

            OpenFinVersion = openFinVersion;
            this.windowName = windowName;
            this.componentType = componentType;
            this.window = window;
            this.uuid = uuid;
        }

        /// <summary>
        /// Connect to Finsemble.
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

                //this.uuid = runtime.Options.UUID;

                routerClient = new RouterClient(this);
                
                storageClient = new StorageClient(this);
                authenticationClient = new AuthenticationClient(this);
                configClient = new ConfigClient(this); 
                windowClient = new WindowClient(this); 
                launcherClient = new LauncherClient(this); 
                distributedStoreClient = new DistributedStoreClient(this);
                linkerClient = new LinkerClient(this);
                

                docking = new Docking(this, windowName + "-channel");

                // Notify listeners that connection is complete.
                // ToDo, wait for clients to be ready??
                Connected?.Invoke(this, EventArgs.Empty);
            });
        }

        /// <summary>
        /// Use this command to execute Finsemble API calls remotely. Specify all the parameters as part of the parameters object and the callback for the callback or eventHandler.
        /// 
        /// Supported API Calls:
        /// <list type="bullet">
        /// <item><term>RouterClient.transmit: </term> <description>same as Javascript API</description></item>
        /// <item><term>RouterClient.addListener: </term> <description>same as Javascript API</description></item>
        /// <item><term>RouterClient.removeListener: </term> <description>same as Javascript API</description></item>
        /// <item><term>RouterClient.publish: </term> <description>same as Javascript API</description></item>
        /// <item><term>RouterClient.subscribe: </term> <description>does not return a subscribeID</description></item>
        /// <item><term>RouterClient.unsubscribe: </term> <description>takes parameters["topic"] and the same callback that was passed to subscribe.</description></item>
        /// <item><term>RouterClient.query: </term> <description>same as Javascript API</description></item>
        /// <item><term>LinkerClient.publish: </term> <description>does not use the callback, does not support the channels option.</description></item>
        /// <item><term>LinkerClient.subscribe: </term> <description>same as Javascript API</description></item>
        /// </list>
        /// </summary>
        /// <example>
        /// <code>
        /// /* The router transmit API has two parameters, toChannel and event */
        /// finsemble.SendCommand("RouterClient.transmit", new JObject {
        ///     ["toChannel"] = "channel",
        ///     ["event"] = new JObject {
        ///         ["myData"] = "myData"
        ///     }
        /// }, (s, args) => {});
        /// 
        /// finsemble.SendCommand("RouterClient.subscribe", new JObject {
        ///     ["topic"] = "myTopic"
        /// }, mySubHandler);
        /// 
        /// finsemble.SendCommand("RouterClient.unsubscribe", new JObject {
        ///     ["topic"] = "myTopic"
        /// }, mySubHandler);
        /// 
        /// /* Linker.publish takes params */
        /// finsemble.SendCommand("LinkerClient.publish", new JObject { 
        ///     ["params"] = new JObject {
        ///         ["dataType"] = "myType",
        ///         ["data"] = new JObject {
        ///             ["property1"] = "property"
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="apiCall">Name of the API call from the list above</param>
        /// <param name="parameters">This is a JObject which contains all the parameters that the API call takes. Refer to our Javascript API for the parameters to each API call.</param>
        /// <param name="callback">If the API has a callback, this will be used to call back.</param>
        public void SendCommand(string apiCall, JObject parameters, EventHandler<FinsembleEventArgs> callback)
        {
            switch(apiCall) {
                case "RouterClient.transmit":                    
                    routerClient.Transmit((string)parameters["toChannel"], parameters["event"]);
                    break;
                case "RouterClient.addListener":
                    routerClient.AddListener((string)parameters["channel"], callback);
                    break;
                case "RouterClient.removeListener":
                    routerClient.RemoveListener((string)parameters["channel"], callback);
                    break;
                case "RouterClient.publish":
                    routerClient.Publish((string)parameters["topic"], parameters["event"]);
                    break;
                case "RouterClient.subscribe":
                    routerClient.Subscribe((string)parameters["topic"], callback);
                    break;
                case "RouterClient.unsubscribe":
                    routerClient.Unsubscribe((string)parameters["topic"], callback);
                    break;
                case "RouterClient.query":
                    routerClient.Query((string)parameters["responderChannel"], parameters["queryEvent"], parameters["params"] as JObject, callback);
                    break;
                case "LinkerClient.publish":
                    linkerClient.Publish(parameters["params"] as JObject);
                    break;
                case "LinkerClient.subscribe":
                    linkerClient.Subscribe((string)parameters["dataType"], callback);
                    break;
                default:
                    throw new Exception("This API does not exist or is not yet supported");
            }
        }
        
        private void Disconnect()
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
        internal void SendRPCCommand(string topic, JObject parameters)
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
        internal void SendRPCCommand(string topic, string parameter, string callbackChannel = "")
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

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        internal virtual void Dispose(bool disposing)
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
