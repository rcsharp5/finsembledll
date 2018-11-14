using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Newtonsoft.Json.Linq;
using Openfin.Desktop;

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
	public partial class Finsemble : IDisposable
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
        public string openFinVersion { get; private set; } = "8.56.28.36";
        #endregion

        public string securityRealm { get; private set; }

        /// <summary>
        /// The instance of the OpenFin used by this example.
        /// </summary>
        internal Runtime runtime { get; private set; }

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

        public delegate void RPCCallbackHandler(JToken error, JToken response);

        #endregion

        public string componentType { private set; get; } = "Unknown";
        public string windowName { private set; get; } = Guid.NewGuid().ToString();
        public string uuid { private set; get; } = Guid.NewGuid().ToString();

        public RouterClient RouterClient { private set; get; }
        internal Logger logger { private set; get; }
        internal AuthenticationClient authenticationClient { private set; get; }
        internal DistributedStoreClient distributedStoreClient { private set; get; }
        internal StorageClient storageClient { private set; get; }
        internal WindowClient windowClient { private set; get; }
        internal LauncherClient launcherClient { private set; get; }
        public LinkerClient LinkerClient { set; get; }
        internal ConfigClient configClient { private set; get; }
        public DragAndDropClient DragAndDropClient { private set; get; }
        public JObject componentConfig { internal set; get; }
        private int openFinConnectionRetryAttempts = 0;

        public System.Windows.Window window { private set; get; }
        internal Docking docking;
        private Dictionary<string, List<string>> dependencies = new Dictionary<string, List<string>>()
        {
            {"distributedStoreClient", new List<string>() {"dataStoreService"}  },
            {"launcherClient", new List<string> {"launcherService"} },
            {"linkerClient", new List<string> {"linkerService"} },
            {"windowClient", new List<string> {"storageService"} }
        };

        private bool isFinsembleConnected = false;

        string top = null, left = null, height = null, width = null;
        /// <summary>
        /// Initializes a new instance of the FinsembleBridge class. This is how you interact with Finsemble. All the clients will be part of the bridge.
        /// </summary>
        public Finsemble(string[] args, System.Windows.Window window)
        {
            if (args.Length > 0)
            {
                for (var i = 0; i < args.Length; i++)
                {
                    if (!args[i].Contains("=")) continue;
                    var argument = args[i].Split(new char[] { '=' }, 2);
                    var argumentName = argument[0];
                    var argumentValue = argument[1];
                    switch (argumentName)
                    {
                        case "top":
                            top = argumentValue;
                            break;
                        case "left":
                            left = argumentValue;
                            break;
                        case "width":
                            width = argumentValue;
                            break;
                        case "height":
                            height = argumentValue;
                            break;
                        case "finsembleWindowName":
                            windowName = argumentValue;
                            break;
                        case "componentType":
                            componentType = argumentValue;
                            break;
                        case "uuid":
                            uuid = argumentValue;
                            break;
                        case "openfinVersion":
                            openFinVersion = argumentValue;
                            break;
                        case "securityRealm":
                            securityRealm = argumentValue;
                            break;
                    }
                }
            }

            Logger.Debug(
                "Initializing new instance of FinsembleBridge:\n" +
                $"\tVersion: {openFinVersion}\n");

            this.window = window;
            if (window != null)
            {
                window.Loaded += Window_Loaded;
            }
        }

        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(top))
            {
                window.Top = Double.Parse(top);
            }

            if (!string.IsNullOrEmpty(left))
            {
                window.Left = Double.Parse(left);
            }

            if (!string.IsNullOrEmpty(height))
            {
                window.Height = Double.Parse(height);
            }

            if (!string.IsNullOrEmpty(width))
            {
                window.Width = Double.Parse(width);
            }
        }

        private void OFConnect()
        {
            try
            {
                runtime.Connect(() =>
                {
                    Logger.Info("Connected to OpenFin Runtime.");

                    //this.uuid = runtime.Options.UUID;

                    RouterClient = new RouterClient(this, Connect);

                    RouterClient.Init();
                });
                openFinConnectionRetryAttempts++;
            }
            catch (Exception e)
            {
                if (openFinConnectionRetryAttempts < 5)
                {
                    OFConnect();
                }
                else
                {
                    throw e;
                }
            }
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
                Version = openFinVersion,
            };

            if (!String.IsNullOrEmpty(securityRealm))
            {
                runtimeOptions.SecurityRealm = securityRealm;
            }

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
            OFConnect();
        }

        public void HandleClose(Action<Action> callOnClose)
        {
            docking.closeAction = callOnClose;
        }

        private void Connect(object sender, bool connected)
        {
            // Do not attempt to connect more than once. This causes the Window to close prematurely from responders firing because of errors.
            if (isFinsembleConnected) return;
            isFinsembleConnected = true;
            logger = new Logger(this);
            storageClient = new StorageClient(this);
            authenticationClient = new AuthenticationClient(this);
            configClient = new ConfigClient(this);
            configClient.GetValue(new JObject
            {
                ["field"] = "finsemble.components." + this.componentType
            }, (s, a) =>
            {
                this.componentConfig = (JObject)a.response["data"];
                if (this.componentConfig == null) this.componentConfig = new JObject();
                windowClient = new WindowClient(this);
                launcherClient = new LauncherClient(this);
                distributedStoreClient = new DistributedStoreClient(this);
                if (window != null) DragAndDropClient = new DragAndDropClient(this);
                if (window != null) docking = new Docking(this, windowName + "-channel");

                LinkerClient = new LinkerClient(this, (s2, a2) => {
                    Connected?.Invoke(this, EventArgs.Empty);
                });

                // Notify listeners that connection is complete.
                // ToDo, wait for clients to be ready??

            });


        }
        /// <summary>
        /// Use this command to execute Finsemble API calls remotely. Specify all the arguments as a list and the callback for the callback or eventHandler.
        /// 
        /// Supported API Calls:
        /// <list type="bullet">
        /// <item><term>RouterClient.transmit: </term> <description>same as JavaScript API</description></item>
        /// <item><term>RouterClient.addListener: </term> <description>same as JavaScript API</description></item>
        /// <item><term>RouterClient.removeListener: </term> <description>same as JavaScript API</description></item>
        /// <item><term>RouterClient.publish: </term> <description>same as JavaScript API</description></item>
        /// <item><term>RouterClient.subscribe: </term> <description>does not return a subscribeID</description></item>
        /// <item><term>RouterClient.unsubscribe: </term> <description>takes parameters["topic"] and the same callback that was passed to subscribe.</description></item>
        /// <item><term>RouterClient.query: </term> <description>same as JavaScript API</description></item>
        /// <item><term>LinkerClient.publish: </term> <description>does not use the callback, does not support the channels option.</description></item>
        /// <item><term>LinkerClient.subscribe: </term> <description>same as JavaScript API</description></item>
        /// <item><term>LauncherClient.spawn: </term> <description>same as JavaScript API</description></item>
        /// <item><term>LauncherClient.showWindow: </term> <description>same as JavaScript API</description></item>
        /// <item><term>ConfigClient.getValue: </term> <description>same as JavaScript API</description></item>
        /// </list>
        /// </summary>
        /// <example>
        /// <code>
        /// /* The router transmit API has two parameters, toChannel and event */
        /// finsemble.SendRPCMessage("RouterClient.transmit", new List&lt;JToken&gt; {
        ///     "channel",
        ///     new JObject {
        ///         ["myData"] = "myData"
        ///     }
        /// }, (s, args) => {});
        /// 
        /// finsemble.SendRPCMessage("RouterClient.subscribe", new List&lt;JToken&gt; {
        ///     "myTopic"
        /// }, mySubHandler);
        /// 
        /// finsemble.SendRPCMessage("RouterClient.unsubscribe", new List&lt;JToken&gt; {
        ///     "myTopic"
        /// }, mySubHandler);
        /// 
        /// /* Linker.publish takes params */
        /// finsemble.SendCommand("LinkerClient.publish", new List&lt;JToken&gt; { 
        ///     new JObject {
        ///         ["dataType"] = "myType",
        ///         ["data"] = new JObject {
        ///             ["property1"] = "property"
        ///         }
        ///     }
        /// }, (s, args) => {});
        /// </code>
        /// </example>
        /// <param name="endpoint">Name of the API call from the list above</param>
        /// <param name="args">This is a JObject which contains all the parameters that the API call takes. Refer to our JavaScript API for the parameters to each API call.</param>
        /// <param name="cb">If the API has a callback, this will be used to call back.</param>
        public void RPC(string endpoint, JArray args, RPCCallbackHandler cb = null)
        {
            var l = new List<JToken>();
            foreach (var item in args)
            {
                l.Add(item);
            }
            RPC(endpoint, l, (s, a) => {
                cb?.Invoke(a.error, a.response);
            });
        }

        public void RPC(string endpoint, List<JToken> args, RPCCallbackHandler cb = null)
        {
            RPC(endpoint, args, (s, a) =>
            {
                cb?.Invoke(a.error, a.response);
            });
        }

        private void RPC(string endpoint, List<JToken> args, EventHandler<FinsembleEventArgs> cb)
        {
            switch (endpoint)
            {
                case "RouterClient.transmit":
                    RouterClient.Transmit((string)args[0], args[1]);
                    break;
                case "RouterClient.addListener":
                    RouterClient.AddListener((string)args[0], cb);
                    break;
                case "RouterClient.removeListener":
                    RouterClient.RemoveListener((string)args[0], cb);
                    break;
                case "RouterClient.publish":
                    RouterClient.Publish((string)args[0], args[1]);
                    break;
                case "RouterClient.subscribe":
                    RouterClient.Subscribe((string)args[0], cb);
                    break;
                case "RouterClient.unsubscribe":
                    RouterClient.Unsubscribe((string)args[0], cb);
                    break;
                case "RouterClient.query":
                    RouterClient.Query((string)args[0], args[1], args[2] as JObject, cb);
                    break;
                case "LinkerClient.publish":
                    LinkerClient.Publish(args[0] as JObject);
                    break;
                case "LinkerClient.subscribe":
                    LinkerClient.Subscribe((string)args[0], cb);
                    break;
                case "LauncherClient.spawn":
                    launcherClient.Spawn((string)args[0], args[1] as JObject, cb);
                    break;
                case "LauncherClient.showWindow":
                    launcherClient.ShowWindow(args[0] as JObject, args[1] as JObject, cb);
                    break;
                case "ConfigClient.getValue":
                    configClient.GetValue(args[0] as JObject, cb);
                    break;
                case "AuthenticationClient.publishAuthorization":
                    authenticationClient.PublishAuthorization<JObject>((string)args[0], args[1] as JObject);
                    break;
                case "AuthenticationClient.getCurrentCredentials":
                    authenticationClient.GetCurrentCredentials<JObject>((s, a) =>
                    {
                        cb(this, new FinsembleEventArgs(null, a));
                    });
                    break;
                case "Logger.error":
                    JToken[] argsArray = args.ToArray();
                    logger.Error(argsArray);
                    break;
                case "Logger.warn":
                    logger.Warn(args.ToArray());
                    break;
                case "Logger.log":
                    logger.Log(args.ToArray());
                    break;
                case "Logger.info":
                    logger.Info(args.ToArray());
                    break;
                case "Logger.debug":
                    logger.Debug(args.ToArray());
                    break;
                case "Logger.verbose":
                    logger.Verbose(args.ToArray());
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

        public void ShutdownApplication()
        {
            RouterClient.Transmit("Application.shutdown", new JObject { });
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
