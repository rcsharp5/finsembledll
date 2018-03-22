using System;

namespace ChartIQ.Finsemble
{
	internal class CallbackChannel
    {
        /// <summary>
        /// GUID for the CallbackChannel instance.
        /// </summary>
        private string guid = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets a string that represents the subscribe callback channel name for this instance.
        /// </summary>
        public string Subscribe { get; private set; }

        /// <summary>
        /// Initializes a new instance of the CallbackChannel class.
        /// </summary>
        public CallbackChannel()
        {
            Subscribe = string.Format("SUBSCRIBE-{0}", guid);
        }
    }
}
