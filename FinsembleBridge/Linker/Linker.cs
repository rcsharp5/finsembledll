using System.Dynamic;

namespace ChartIQ.Finsemble
{
	public class Linker
    {
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
    }
}
