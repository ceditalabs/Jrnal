using Chic.Attributes;
using Chic.Constraints;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cedita.Labs.Jrnal.Db.Models
{
    [Table("Properties")]
    public class Property : IKeyedEntity, IHaveAotMarker, IHaveChildAotMarker
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public int? ParentPropertyId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public int AotInsertionMarker { get; set; }
        [DbIgnore]
        public int AotParentMarker { get; set; }

        [DbIgnore]
        public virtual ICollection<Property> Children { get; set; }
    }
}
