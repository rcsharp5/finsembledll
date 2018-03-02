using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartIQ.Finsemble
{
    public class RouterClient
    {
        private FinsembleBridge bridge;
        private string clientName;
        private Dictionary<string, EventHandler<FinsembleEventArgs>> transmitListeners = new Dictionary<string, EventHandler<FinsembleEventArgs>>();
        private Dictionary<string, EventHandler<FinsembleEventArgs>> publishListeners = new Dictionary<string, EventHandler<FinsembleEventArgs>>();
        private Dictionary<string, EventHandler<FinsembleEventArgs>> queryIDResponseHandlerMap = new Dictionary<string, EventHandler<FinsembleEventArgs>>();

        public RouterClient(FinsembleBridge bridge)
        {
            this.bridge = bridge;
            this.clientName = "RouterClient." + bridge.windowName;
            var Handshake = new JObject(
                new JProperty("header",
                    new JObject(
                        new JProperty("origin", clientName),
                        new JProperty("type", "initialHandshake")
                    )
                )
            );
            bridge.runtime.InterApplicationBus.Publish("RouterService", Handshake); //TODO: wait for handshake response
            bridge.runtime.InterApplicationBus.subscribe(clientName, OpenfinMessageHandler);
        }

        // All messages from Finsemble are handled by this.
        private void OpenfinMessageHandler(string sourceUuid, string topic, object message)
        {
            
            dynamic m = JsonConvert.DeserializeObject(message.ToString());
            FinsembleEventArgs args;
            switch (m.header.type.Value)
            {
                case "transmit":
                    args = new FinsembleEventArgs(null, message as JObject);
                    transmitListeners[m.header.channel.Value]?.Invoke(this, args);
                    break;
                case "queryResponse":
                    args = new FinsembleEventArgs(null, message as JObject); // TODO: Handle Errors
                    queryIDResponseHandlerMap[m.header.queryID.Value]?.Invoke(this, args);
                    queryIDResponseHandlerMap.Remove(m.header.queryID.Value);
                    break;
                case "publish":
                    args = new FinsembleEventArgs(null, message as JObject);
                    publishListeners[m.header.topic.Value]?.Invoke(this, args);
                    break;
            }
        }

        // Transmit/Listen
        public void transmit(string channel, JObject data)
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
            bridge.runtime.InterApplicationBus.Publish("RouterService", TransmitMessage);
        }

        public void addListener(string channel, EventHandler<FinsembleEventArgs> callback)
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
                bridge.runtime.InterApplicationBus.Publish("RouterService", AddListenerMessage);
            }
            else
            {
                transmitListeners[channel] += callback;
            }
        }

        public void removeListener(string channel, EventHandler<FinsembleEventArgs> callback)
        {
            transmitListeners[channel] -= callback;
        }

        // Query Response
        public void query(string channel, JObject data, JObject parameters, EventHandler<FinsembleEventArgs> responseHandler)
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
            bridge.runtime.InterApplicationBus.Publish("RouterService", QueryMessage);
            queryIDResponseHandlerMap.Add(queryID, responseHandler);
        }

        public void addResponder(string channel, EventHandler<FinsembleEventArgs> callback)
        {

        }

        // Pub Sub
        public void publish(string topic, JObject data)
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
            bridge.runtime.InterApplicationBus.Publish("RouterService", PublishMessage);
        }

        public void subscribe(string topic, EventHandler<FinsembleEventArgs> responseHandler)
        {
            if (!publishListeners.ContainsKey(topic))
            {
                publishListeners.Add(topic, responseHandler);
                var AddSubscribeMessage = new JObject(
                   new JProperty("header",
                       new JObject(
                           new JProperty("origin", clientName),
                           new JProperty("type", "subscribe"),
                           new JProperty("topic", topic),
                           new JProperty("subscribeID", Guid.NewGuid().ToString())
                       )
                   )
                );
                bridge.runtime.InterApplicationBus.Publish("RouterService", AddSubscribeMessage);
            }
            else
            {
                publishListeners[topic] += responseHandler;
            }
        }

        public void unsubscribe(string topic, EventHandler<FinsembleEventArgs> responseHandler)
        {
            publishListeners[topic] -= responseHandler;
        }
        
    }
}
