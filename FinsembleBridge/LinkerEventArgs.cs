using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartIQ.Finsemble
{
    public class LinkerEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the UUID of the source application.
        /// </summary>
        public string SourceUuid { get; private set; }

        /// <summary>
        /// Gets the topic associated with the linker event.
        /// </summary>
        public string Topic { get; private set; }

        /// <summary>
        /// Gets the messages associated with the linker event.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Initializes a new instance of the LInkerEventArgs class.
        /// </summary>
        /// <param name="sourceUuid">The UUID of the source application</param>
        /// <param name="topic">The topic associated with the message</param>
        /// <param name="message">The message</param>
        public LinkerEventArgs(string sourceUuid, string topic, string message)
        {
            SourceUuid = sourceUuid;
            Topic = topic;
            Message = message;
        }
    }

}
