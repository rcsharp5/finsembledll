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
            bridge.runtime.InterApplicationBus.Publish("RouterService", Handshake); //TODO -> wait for handshake response
            bridge.runtime.InterApplicationBus.subscribe(clientName, OpenfinMessageHandler);
        }

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

        private void OpenfinMessageHandler(string sourceUuid, string topic, object message)
        {
            
            dynamic m = JsonConvert.DeserializeObject(message.ToString());
            switch (m.header.type.Value)
            {
                case "transmit":
                    FinsembleEventArgs args = new FinsembleEventArgs(null, message as JObject);
                    transmitListeners[m.header.channel.Value]?.Invoke(this, args);
                    break;
            }
            


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



    }
}
