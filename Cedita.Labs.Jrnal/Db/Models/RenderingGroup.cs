using Chic.Attributes;
using Chic.Constraints;
using System.Collections.Generic;

namespace Cedita.Labs.Jrnal.Db.Models
{
    public class RenderingGroup : IKeyedEntity, IHaveAotMarker, IHaveChildAotMarker
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string Name { get; set; }

        public int AotInsertionMarker { get; set; }
        [DbIgnore]
        public int AotParentMarker { get; set; }

        [DbIgnore]
        public virtual ICollection<SingleRendering> Renderings { get; set; }
    }
}
