using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Timer = System.Timers.Timer;

namespace ChartIQ.Finsemble
{
	/// <summary>
	/// This module contains the RouterClient for sending and receiving events between Finsemble components and services.
	/// Currently AddPubSubResponder, AddResponder, RemovePubSubResponder, RemoveResponder are not supported.
	/// </summary>
	public class RouterClient : IDisposable
	{
		private Finsemble bridge;
		private string clientName;
		private Dictionary<string, EventHandler<FinsembleEventArgs>> transmitListeners = new Dictionary<string, EventHandler<FinsembleEventArgs>>();
		private Dictionary<string, EventHandler<FinsembleEventArgs>> publishListeners = new Dictionary<string, EventHandler<FinsembleEventArgs>>();
		private Dictionary<string, EventHandler<FinsembleEventArgs>> queryIDResponseHandlerMap = new Dictionary<string, EventHandler<FinsembleEventArgs>>();
		private Dictionary<string, EventHandler<FinsembleQueryArgs>> responderMap = new Dictionary<string, EventHandler<FinsembleQueryArgs>>();
		private Dictionary<string, string> subscribeIDTopicMap = new Dictionary<string, string>();
		private EventHandler<bool> connectHandler;
		private bool connected = false;

		internal void Init()
		{
			this.clientName = "RouterClient." + bridge.windowName;
			var Handshake = new JObject(
				new JProperty("header",
					new JObject(
						new JProperty("origin", clientName),
						new JProperty("type", "initialHandshake")
					)
				)
			);
			bridge.Publish("RouterService", Handshake); //TODO: wait for handshake response
			bridge.Subscribe(clientName, MessageHandler);

			var timer = new Timer(100);
			timer.Elapsed += (s, e) =>
			{
				if (!connected) //retry handshake until connected
				{
					bridge.Publish("RouterService", Handshake); //TODO: wait for handshake response
				}
				else
				{
					timer.AutoReset = false;
					timer.Enabled = false;
				}
			};
			timer.AutoReset = true;
			timer.Enabled = true;

		}
		internal RouterClient(Finsemble bridge, EventHandler<bool> connectHandler)
		{
			this.bridge = bridge;
			this.connectHandler = connectHandler;
		}

		// All messages from Finsemble are handled by this.
		private void MessageHandler(string topic, object message)
		{

			dynamic m = JsonConvert.DeserializeObject(message.ToString());
			FinsembleEventArgs args;
			FinsembleQueryArgs qargs;
			var type = m.header.type.Value as string;
			switch (type)
			{
				case "transmit":
					args = new FinsembleEventArgs(null, message as JObject);
					if (transmitListeners.ContainsKey(m.header.channel.Value))
					{
						transmitListeners[m.header.channel.Value]?.Invoke(this, args);
					}
					break;
				case "query":
					qargs = new FinsembleQueryArgs(null, message as JObject, (e) =>
					{
						var queryMessage = new JObject
						{
							["header"] = new JObject
							{
								["origin"] = clientName,
								["type"] = "queryResponse",
								["queryID"] = m.header.queryID,
								["error"] = m.error
							},
							["data"] = e
						};
						bridge.Publish("RouterService", queryMessage);
					});
					if (responderMap.ContainsKey(m.header.channel.Value))
					{
						responderMap[m.header.channel.Value]?.Invoke(this, qargs);
					}
					break;
				case "queryResponse":
					args = new FinsembleEventArgs(null, message as JObject); // TODO: Handle Errors
					if (queryIDResponseHandlerMap.ContainsKey(m.header.queryID.Value))
					{
						queryIDResponseHandlerMap[m.header.queryID.Value]?.Invoke(this, args);
						queryIDResponseHandlerMap.Remove(m.header.queryID.Value);
					}
					break;
				case "notify":
					args = new FinsembleEventArgs(null, message as JObject);
					if (publishListeners.ContainsKey(m.header.topic.Value))
					{
						publishListeners[m.header.topic.Value]?.Invoke(this, args);
					}
					break;
				case "initialHandshakeResponse":
					if (connected) break;
					Debug.WriteLine("Router Connected");
					connected = true;
					if (bridge.window != null)
					{

						Application.Current.Dispatcher.Invoke(delegate //main thread
						{
							this.bridge.window.Loaded += (s, a) =>
							{
								var handle = (new WindowInteropHelper(this.bridge.window).Handle).ToString("X");
								this.Transmit("Finsemble.Assimilation.register", new JObject
								{
									["windowName"] = this.bridge.windowName,
									["windowHandle"] = handle
								});
							};
						});
					} else
                    {
                        this.Transmit("Finsemble.Assimilation.register", new JObject
                        {
                            ["windowName"] = this.bridge.windowName,
                            ["windowHandle"] = null
                        });
                        // Quit WindowLess Application
                        this.AddListener("Finsemble.Assimilation.close." + this.bridge.windowName, (s, a) =>
                        {
                            Environment.Exit(0);
                        });
                    }

					connectHandler(this, true);
					break;
			}
		}

		/// <summary>
		/// Transmit data to all listeners on the specified channel. If no listeners the data is discarded without error. All listeners to the channel in this Finsemble window and other Finsemble windows will receive the transmit.
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="data"></param>
		public void Transmit(string channel, JToken data)
		{
			var TransmitMessage = new JObject(
				new JProperty("header",
					new JObject(
						new JProperty("origin", clientName),
						new JProperty("type", "transmit"),
						new JProperty("channel", channel)
					)
				),
				new JProperty("data", data)
			);
			bridge.Publish("RouterService", TransmitMessage);
		}

		/// <summary>
		/// Add listener for incoming data on specified channel. Each of the incoming data will trigger the specified handler.
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="callback"></param>
		public void AddListener(string channel, EventHandler<FinsembleEventArgs> callback)
		{
			if (!transmitListeners.ContainsKey(channel))
			{
				transmitListeners.Add(channel, callback);
				var AddListenerMessage = new JObject(
				   new JProperty("header",
					   new JObject(
						   new JProperty("origin", clientName),
						   new JProperty("type", "addListener"),
						   new JProperty("channel", channel)
					   )
				   )
				);
				bridge.Publish("RouterService", AddListenerMessage);
			}
			else
			{
				transmitListeners[channel] += callback;
			}
		}

		/// <summary>
		/// Remove listener from specified channel for the specific data handler (only listeners created locally can be removed).
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="callback"></param>
		public void RemoveListener(string channel, EventHandler<FinsembleEventArgs> callback)
		{
			transmitListeners[channel] -= callback;
			if (transmitListeners.Count == 0)
			{
				var removeListenerMessage = new JObject
				{
					["header"] = new JObject
					{
						["origin"] = clientName,
						["type"] = "removeListener",
						["channel"] = channel
					}
				};
				bridge.Publish("RouterService", removeListenerMessage);
			}
		}

		/// <summary>
		/// Send a query to responder listening on specified channel. The responder may be in this Finsemble window or another Finsemble window.
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="data"></param>
		/// <param name="parameters"></param>
		/// <param name="responseHandler"></param>
		public void Query(string channel, JToken data, JObject parameters, EventHandler<FinsembleEventArgs> responseHandler)
		{
			var queryID = Guid.NewGuid().ToString();
			var QueryMessage = new JObject(
				new JProperty("header",
					new JObject(
						new JProperty("origin", clientName),
						new JProperty("type", "query"),
						new JProperty("queryID", queryID),
						new JProperty("channel", channel)
					)
				),
				new JProperty("data", data)
			);
			bridge.Publish("RouterService", QueryMessage);
			queryIDResponseHandlerMap.Add(queryID, responseHandler);
		}

		/// <summary>
		/// Publish to a PubSub Responder, which will trigger a corresponding Notify to be sent to all subscribers (local in this window or remote in other windows). There can be multiple publishers for a topic (again, in same window or remote windows)
		/// </summary>
		/// <param name="topic"></param>
		/// <param name="data"></param>
		public void Publish(string topic, JToken data)
		{
			var PublishMessage = new JObject(
				new JProperty("header",
					new JObject(
						new JProperty("origin", clientName),
						new JProperty("type", "publish"),
						new JProperty("channel", topic)
					)
				),
				new JProperty("data", data)
			);
			bridge.Publish("RouterService", PublishMessage);
		}

		/// <summary>
		/// Subscribe to a PubSub Responder. Each responder topic can have many subscribers (local in this window or remote in other windows). Each subscriber immediately (but asynchronously) receives back current state in a notify; new notifies are receive for each publish sent to the same topic.
		/// </summary>
		/// <param name="topic"></param>
		/// <param name="responseHandler"></param>
		public void Subscribe(string topic, EventHandler<FinsembleEventArgs> responseHandler)
		{
			if (!publishListeners.ContainsKey(topic))
			{
				publishListeners.Add(topic, responseHandler);
				var subscribeId = Guid.NewGuid().ToString();
				subscribeIDTopicMap.Add(topic, subscribeId);
				var AddSubscribeMessage = new JObject(
				   new JProperty("header",
					   new JObject(
						   new JProperty("origin", clientName),
						   new JProperty("type", "subscribe"),
						   new JProperty("topic", topic),
						   new JProperty("subscribeID", subscribeId)
					   )
				   )
				);
				bridge.Publish("RouterService", AddSubscribeMessage);
			}
			else
			{
				publishListeners[topic] += responseHandler;
			}
		}

		/// <summary>
		/// Unsubscribe from PubSub responder so no more notifications received (but doesn't affect other subscriptions).
		/// </summary>
		/// <param name="topic"></param>
		/// <param name="responseHandler"></param>
		public void Unsubscribe(string topic, EventHandler<FinsembleEventArgs> responseHandler)
		{
			publishListeners[topic] -= responseHandler;
			if (publishListeners.Count == 0)
			{
				var unsubscribeMessage = new JObject
				{
					["header"] = new JObject
					{
						["origin"] = clientName,
						["type"] = "unsubscribe",
						["subscribeID"] = subscribeIDTopicMap[topic],
						["topic"] = topic
					}
				};
				bridge.Publish("RouterService", unsubscribeMessage);
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="responseHandler"></param>
		public void AddResponder(string channel, EventHandler<FinsembleQueryArgs> responseHandler)
		{
			if (!responderMap.ContainsKey(channel))
			{
				responderMap.Add(channel, responseHandler);
				var AddResponderMessage = new JObject
				{
					["header"] = new JObject
					{
						["origin"] = clientName,
						["type"] = "addResponder",
						["channel"] = channel
					}
				};
				bridge.Publish("RouterService", AddResponderMessage);
			}
			else
			{
				responseHandler(this, new FinsembleQueryArgs(new JObject { ["error"] = "Responder Already Exists" }, null, null));
			}
		}

		public void RemoveResponder(string channel, bool modifyCollection = true)
		{
			if (responderMap.ContainsKey(channel))
			{
				if (modifyCollection) responderMap.Remove(channel);
				var removeResponderMessage = new JObject
				{
					["header"] = new JObject
					{
						["origin"] = clientName,
						["type"] = "removeResponder",
						["channel"] = channel
					}
				};
				bridge.Publish("RouterService", removeResponderMessage);
			}
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

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.
				foreach (var item in transmitListeners)
				{
					var RemoveListenerMessage = new JObject(
					   new JProperty("header",
						   new JObject(
							   new JProperty("origin", clientName),
							   new JProperty("type", "removeListener"),
							   new JProperty("channel", item.Key)
						   )
					   )
					);
					bridge.Publish("RouterService", RemoveListenerMessage);
				}

				foreach (var item in publishListeners)
				{
					var RemoveListenerMessage = new JObject(
					   new JProperty("header",
						   new JObject(
							   new JProperty("origin", clientName),
							   new JProperty("type", "unsubscribe"),
							   new JProperty("topic", item.Key),
							   new JProperty("subscribeID", subscribeIDTopicMap[item.Key])
						   )
					   )
					);
					bridge.Publish("RouterService", RemoveListenerMessage);
				}

				foreach (var item in responderMap)
				{
					RemoveResponder(item.Key, false);
				}


				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		~RouterClient()
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
