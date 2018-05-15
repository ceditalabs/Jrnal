using Chic.Constraints;

namespace Cedita.Labs.Jrnal.Db.Models
{
    public class Application : IKeyedEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ApiKey { get; set; }
    }
}
