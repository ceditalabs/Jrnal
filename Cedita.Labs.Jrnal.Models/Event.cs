using System;
using System.Collections.Generic;

namespace Cedita.Labs.Jrnal.Models
{
    public enum EventTypeCategory
    {
        Application,
        Error,
        Mail
    }

    public class Event
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Level { get; set; }
        public string MessageTemplate { get; set; }
        public Exception Exception { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public Dictionary<string, List<SingleRendering>> Renderings { get; set; }
    }
}
