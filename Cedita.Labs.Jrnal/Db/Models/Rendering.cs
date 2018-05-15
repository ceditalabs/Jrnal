using Chic.Attributes;
using Chic.Constraints;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cedita.Labs.Jrnal.Db.Models
{
    [Table("Renderings")]
    public class SingleRendering : IKeyedEntity, IHaveChildAotMarker
    {
        public int Id { get; set; }
        public int RenderingGroupId { get; set; }
        public string Format { get; set; }
        public string Rendering { get; set; }

        [DbIgnore]
        public int AotParentMarker { get; set; }
    }
}
