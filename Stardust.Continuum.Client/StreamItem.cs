using System;
using System.Collections.Generic;

namespace Stardust.Continuum.Client
{
    public class StreamItem
    {
        /// <summary>
        /// This is set by the service if not provided
        /// </summary>
        /// <value>
        /// The timestamp.
        /// </value>
        public DateTime? Timestamp { get; set; }

        public string UserName { get; set; }

        public string CorrelationToken { get; set; }

        public string Message { get; set; }

        public string StackTrace { get; set; }

        public LogLevels LogLevel { get; set; }

        public string ServiceName { get; set; }

        public string Environment { get; set; }

        
        public Dictionary<string,object> Properties { get; set; }
    }
}