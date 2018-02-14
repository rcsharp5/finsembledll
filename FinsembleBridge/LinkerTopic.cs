namespace ChartIQ.Finsemble
{
    public static class LinkerTopic
    {
        /// <summary>
        /// The name of the topic for adding to a group.
        /// </summary>
        public static readonly string AddToGroup = "FSBL.Clients.LinkerClient.addToGroup";

        /// <summary>
        /// The name of the topic for removing from a group.
        /// </summary>
        public static readonly string RemoveFromGroup = "FSBL.Clients.LinkerClient.removeFromGroup";

        /// <summary>
        /// The name of the topic for publishing.
        /// </summary>
        public static readonly string Publish = "FSBL.Clients.LinkerClient.publish";

        /// <summary>
        /// The name of the topic for subscribe.
        /// </summary>
        public static readonly string Subscribe = "FSBL.Clients.LinkerClient.subscribe";

        /// <summary>
        /// The name of the topic for unsubscribe.
        /// </summary>
        public static readonly string Unsubscribe = "FSBL.Clients.LinkerClient.unsubscribe";
    }

}
