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
        public JObject response { get; private set; }

        /// <summary>
        /// Initializes a new instance of the LInkerEventArgs class.
        /// </summary>
        /// <param name="error">Object describing the error, if any or null</param>
        /// <param name="response">Response from the call</param>
        public FinsembleEventArgs(JObject error, JObject response)
        {
            this.error = error;
            this.response = response;
        }
    }
}
