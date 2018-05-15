using Chic.Attributes;
using Chic.Constraints;
using System;
using System.Collections.Generic;

namespace Cedita.Labs.Jrnal.Db.Models
{
    public class Event : IKeyedEntity, IHaveAotMarker
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }

        public DateTimeOffset Timestamp { get; set; }
        public string Level { get; set; }
        public string MessageTemplate { get; set; }

        public bool HasException { get; set; }
        public int AotInsertionMarker { get; set; }

        [DbIgnore]
        public virtual ICollection<Property> Properties { get; set; }

        [DbIgnore]
        public virtual ICollection<RenderingGroup> RenderingGroups { get; set; }
    }
}
