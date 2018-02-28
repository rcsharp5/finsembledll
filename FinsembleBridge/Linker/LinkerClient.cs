using System.Dynamic;
using ChartIQ.Finsemble;
using Newtonsoft.Json.Linq;

namespace ChartIQ.Finsemble
{
    public class LinkerClient
    {
        private FinsembleBridge bridge;
        private static dynamic _Topic = new ExpandoObject();
        public static dynamic Topic
        {
            get
            {
                _Topic.AddToGroup = "FSBL.Clients.LinkerClient.addToGroup";
                _Topic.RemoveFromGroup = "FSBL.Clients.LinkerClient.removeFromGroup";
                _Topic.Publish = "FSBL.Clients.LinkerClient.publish";
                _Topic.Subscribe = "FSBL.Clients.LinkerClient.subscribe";
                _Topic.Unsubscribe = "FSBL.Clients.LinkerClient.unsubscribe";
                _Topic.GetAllGroups = "FSBL.Clients.LinkerClient.getAllGroups";

                return _Topic;
            }
        }

        public LinkerClient(FinsembleBridge bridge)
        {
            this.bridge = bridge;
        }

        public void linkToChannel(string groupName)
        {
            bridge.SendRPCCommand(Topic.AddToGroup, groupName);
        }

        public void unlinkFromChannel(string groupName)
        {
            bridge.SendRPCCommand(Topic.RemoveFromGroup, groupName);
        }

        public void publish(JObject data)
        {
            bridge.SendRPCCommand(Topic.Publish, data);
        }

        public void subscribe(string channel)
        {
            bridge.SendRPCCommand(Topic.Subscribe, "symbol", bridge.CallbackChannel.Subscribe);
        }

        public void unsubscribe(string channel)
        {
            //TODO
        }

        public void getAllChannels(Openfin.Desktop.InterAppMessageHandler callback)
        {
            this.bridge.SubscribeToChannel("allGroupsChannel", callback);
            this.bridge.SendRPCCommand(Topic.GetAllGroups, new JObject().ToString(), "allGroupsChannel");
        }

    }
}
