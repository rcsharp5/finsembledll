using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using log4net;
using Newtonsoft.Json.Linq;
using Openfin.Desktop;
using WebSocket4Net;

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
		private Runtime Runtime;

		/// <summary>
		/// The web socket connection used when IAC is enabled.
		/// </summary>
		private WebSocket socket;

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
		public bool useIAC { get; private set; } = false;
		public string serverAddress { get; private set; } = "";

		public RouterClient RouterClient { private set; get; }
		internal Logger logger { private set; get; }
		internal AuthenticationClient authenticationClient { private set; get; }
		internal DistributedStoreClient distributedStoreClient { private set; get; }
		internal StorageClient storageClient { private set; get; }
		internal WindowClient windowClient { private set; get; }
		internal LauncherClient launcherClient { private set; get; }
		public LinkerClient LinkerClient { set; get; }
		public ConfigClient ConfigClient { private set; get; }
		public DragAndDropClient DragAndDropClient { private set; get; }
		public JObject componentConfig { internal set; get; }
		private int retryAttempts = 0;

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
        int socketRetryAttempts = 0;
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
					if (argumentName.StartsWith("\"") && !argumentName.EndsWith("\""))
					{
						// TODO: Remove this. Horrible hack to work around quotes in arguments
						argumentName = argumentName.Substring(1, argumentName.Length - 1);
					}
					var argumentValue = argument[1];
					if (argumentValue.EndsWith("\"") && !argumentValue.StartsWith("\""))
					{
						// TODO: Remove this. Horrible hack to work around quotes in arguments
						argumentValue = argumentValue.Substring(0, argumentValue.Length - 1);
					}

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
						case "iac":
							try
							{
								useIAC = Boolean.Parse(argumentValue);
							}
							catch
							{
								// If there is an error parsing, default to false.
								useIAC = false;
							}
							break;
						case "serverAddress":
							serverAddress = argumentValue;
							break;
						case "securityRealm":
							securityRealm = argumentValue;
							break;
					}
				}
			}

			Logger.Debug(
				"Initializing new instance of FinsembleBridge:\n" +
				$"\ttop: {top}\n" +
				$"\tleft: {left}\n" +
				$"\twidth: {width}\n" +
				$"\theight: {height}\n" +
				$"\twindowName: {windowName}\n" +
				$"\tcomponentType: {componentType}\n" +
				$"\tUUID: {uuid}\n" +
				(!useIAC ? $"\topenFinVersion: {openFinVersion}\n" : string.Empty) +
				(!useIAC && !string.IsNullOrWhiteSpace(securityRealm) ? $"\tsecurityRealm: {securityRealm}\n" : "") +
				(useIAC ? $"\tuseIAC: {useIAC}\n" : string.Empty) +
				(useIAC ? $"\tserverAddress: {serverAddress}\n" : string.Empty));

			this.window = window;
			if (window != null)
			{
				window.Loaded += Window_Loaded;
			}
		}

		private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
            try
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
            } catch (Exception ex)
            {
                this.Connected += (s, e2) =>
                {
                    this.logger.Error(new JToken[] { "Invalid Bounds", ex.Message, top, left, height, width });
                };

            }
		}

        private void OFConnect()
		{
			Logger.Debug("Connect called");
			if (Runtime != null)
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

			Runtime = Runtime.GetRuntimeInstance(runtimeOptions);

			// Set up error handler.
			Runtime.Error += (s, e) =>
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
			Runtime.Disconnected += (s, e) =>
			{
				Logger.Info("Disconnected from OpenFin runtime.");

				// Notify listeners bridge is disconnected from OpenFin
				Disconnected?.Invoke(this, e);
			};

			try
			{
				Runtime.Connect(() =>
				{
					Logger.Info("Connected to OpenFin Runtime.");

					RouterClient = new RouterClient(this, Connect);

					RouterClient.Init();

					// OpenFin doesn't fire the Disconnected event when Finsemble shuts down. Listen for Application.shutdown event and trigger Disconnect.
					RouterClient.AddListener("Application.shutdown", (s, e) => Disconnect());
				});
				retryAttempts++;
			}
			catch (Exception e)
			{
				if (retryAttempts < 5)
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
			if (useIAC)
			{
				// Connect to Electron using IAC
				ElectronConnect();
			}
			else
			{
				// Connect to the OpenFin runtime.
				OFConnect();
			}
		}

		private void ElectronConnect()
		{
			if (socket != null)
			{
				Logger.Warn("Multiple attempts to connect web socket");
				return;
			}

			if (string.IsNullOrWhiteSpace(serverAddress))
			{
				throw new ArgumentException("IAC is enabled, but no server address was specified.");
			}

			socket = new WebSocket(serverAddress + "/router");

			socket.Opened += (sender, args) =>
			{
				Logger.Info("Web socket connection opened");

				RouterClient = new RouterClient(this, Connect);

				RouterClient.Init();
			};

			socket.Error += (sender, args) =>
			{
                var retryAfter = 100;
                if (socketRetryAttempts >= 20)
                {
                    Logger.Warn("Socket Connection Still Failing after 20 attempts");
                    retryAfter = 500;
                }
                // Keep trying to connect. When large numbers of socket connections are made simultaneously, the connection fails and we get here.
                socketRetryAttempts++;
                Thread.Sleep(retryAfter);
                ElectronConnect();
            };

			socket.Open();

			socket.Closed += (s, e) =>
			{
				Console.WriteLine("Web socket connection disconnected.");

				// Notify listeners bridge is disconnected from OpenFin
				Disconnected?.Invoke(this, EventArgs.Empty);
			};
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="callOnClose"></param>
		/// [Obsolete("HandleClose is deprecated, please use window.Closing instead.")]
		public void HandleClose(Action<Action> callOnClose)
		{
			if (window != null)
			{
				window.Closing += (s, e) => { callOnClose(() => { }); };
			}
		}

		private void Connect(object sender, bool connected)
		{
			// Do not attempt to connect more than once. This causes the Window to close prematurely from responders firing because of errors.
			if (isFinsembleConnected) return;
			isFinsembleConnected = true;

			if (window == null)
			{
				RouterClient.AddListener(string.Format("WindowService-Event-{0}-close-requested", windowName), (s, e) =>
				{
					Disconnected?.Invoke(this, e);
				});
			}

			logger = new Logger(this);
			storageClient = new StorageClient(this);
			authenticationClient = new AuthenticationClient(this);
			ConfigClient = new ConfigClient(this);
			ConfigClient.GetValue(new JObject
			{
				["field"] = "finsemble.components." + this.componentType
			}, (s, a) =>
			{
				if (!(a.response["data"] is JObject))
				{
					// Data isn't a JObject
					logger.Warn(string.Format(
						"Response data object does not have the correct type. Type: \"{0}\", Value: \"{1}\"",
						a.response["data"].GetType().ToString(),
						a.response["data"].ToString()));
				}

				this.componentConfig = a.response["data"] as JObject;
				if (this.componentConfig == null) this.componentConfig = new JObject();
				windowClient = new WindowClient(this);
				launcherClient = new LauncherClient(this);
				distributedStoreClient = new DistributedStoreClient(this);
				if (window != null) DragAndDropClient = new DragAndDropClient(this);
				if (window != null) docking = new Docking(this, windowName + "-channel");

				LinkerClient = new LinkerClient(this, (s2, a2) =>
				{
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
			RPC(endpoint, l, (s, a) =>
			{
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
					ConfigClient.GetValue(args[0] as JObject, cb);
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
			if (disconnecting)
			{
				return;
			}

			disconnecting = true;
			Logger.Info("Disconnect called");

			// Clean up clients that require disposing
			try
			{
				if (logger != null)
				{
					logger.Dispose();
				}
			}
			catch
			{ }
			finally
			{
				logger = null;
			}

			try
			{

				if (RouterClient != null)
				{
                    RouterClient.Transmit("Finsemble.Assimilation.closed." + this.windowName, true);
					RouterClient.Dispose();
				}
			}
			catch { }
			finally
			{
				RouterClient = null;
			}

			// Close appropriate connection
			if (Runtime != null)
			{
				Runtime.Disconnect(() => { });
				Runtime = null;
			}

			if (socket != null)
			{
				socket.Close();
				socket = null;
			}
		}

		internal void Publish(string topic, JObject message)
		{
			if (useIAC)
			{
				if (socket == null)
				{
					throw new InvalidOperationException("Calling socket connection for publish before it is initialized.");
				}

				var routerMessage = new JObject();
				var messageToSend = new JObject();
				if ("RouterService".Equals(topic))
				{
					// Change topic for IAC
					topic = "ROUTER_SERVICE";
				}
				else
				{
					topic = "ROUTER_CLIENT";
					routerMessage["client"] = "RouterClient." + this.windowName;
				}

				// Modifying to meet format expected by the router.

				routerMessage["clientMessage"] = message;
				messageToSend["dest"] = topic;
				messageToSend["message"] = routerMessage;

				socket.Send(messageToSend.ToString());
				//socket.Emit(topic, routerMessage);
			}
			else
			{
				if (Runtime == null)
				{
					throw new InvalidOperationException("Calling OpenFin runtime for publish before it is initialized.");
				}

				Runtime.InterApplicationBus.Publish(topic, message);
			}
		}

		internal void Subscribe(string topic, MessageHandler listener)
		{
			if (useIAC)
			{
				if (socket == null)
				{
					throw new InvalidOperationException("Calling socket connection for subscribe before it is initialized.");
				}

				socket.MessageReceived += (sender, args) =>
				{
					JObject data = JObject.Parse(args.Message);
					if (data["dest"].ToString() != "ROUTER_SERVICE")
					{
						JObject message = (JObject)data["message"];
						if ("RouterClient." + this.windowName == message["client"].ToString())
						{
							JObject clientMessage = (JObject)message["clientMessage"];
							listener(topic, clientMessage);
						}
					}
				};
			}
			else
			{
				if (Runtime == null)
				{
					throw new InvalidOperationException("Calling OpenFin runtime for subscribe before it is initialized.");
				}

				Runtime.InterApplicationBus.subscribe("*", topic, (s, t, m) =>
				{
					var joMessage = m as JObject;
					listener(t, joMessage);
				});
			}
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls
		private bool disconnecting = false;

		internal virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
				}

				Disconnect();

				Runtime = null;

				disposedValue = true;
			}
		}

		public void ShutdownApplication()
		{
			RouterClient.Transmit("Application.shutdown", new JObject { });
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		~Finsemble()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(false);
		}

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

namespace ChartIQ.Finsemble
{
	internal delegate void MessageHandler(string topic, JObject message);
}