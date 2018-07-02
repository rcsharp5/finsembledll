using Newtonsoft.Json.Linq;
using System;

namespace ChartIQ.Finsemble
{
    /// <summary>
    /// Event arguments for linker events.
    /// </summary>
    public class FinsembleEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the UUID of the source application.
        /// </summary>
        public JObject error { get; private set; }

        /// <summary>
        /// Gets the messages associated with the linker event.
        /// </summary>
        public JToken response { get; private set; }

        /// <summary>
        /// Initializes a new instance of the FinsembleEventArgs class.
        /// </summary>
        /// <param name="error">Object describing the error, if any or null</param>
        /// <param name="response">Response from the call</param>
        public FinsembleEventArgs(JObject error, JToken response)
        {
            this.error = error;
            this.response = response;
        }
    }

    public class FinsembleQueryArgs : EventArgs
    {
        public JObject error { get; private set; }
        public JToken response { get; private set; }
        public Action<JToken> sendQueryMessage {get; private set;}

        public FinsembleQueryArgs(JObject error, JToken response, Action<JToken> sendQueryMessage)
        {
            this.error = error;
            this.response = response;
            this.sendQueryMessage = sendQueryMessage;
        }
    }
}
