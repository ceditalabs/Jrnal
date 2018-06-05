using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Cedita.Labs.Jrnal.Db;
using Cedita.Labs.Jrnal.Db.Models;
using Cedita.Labs.Jrnal.Models.Containers;
using Chic.Abstractions;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Cedita.Labs.Jrnal.Controllers.Web
{
    [Route("api/web/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IDbConnection db;
        public EventsController(IDbConnection db)
        {
            this.db = db;
        }

        // GET api/values
        [HttpGet("ByRequest")]
        public async Task<IActionResult> Post(int maxRequests = 10)
        {
            var requestIds = await db.QueryAsync<string>($@"SELECT
    TOP {maxRequests}
    p.Value
FROM Properties p
INNER JOIN Events e ON e.Id = p.EventId
WHERE p.Name = 'RequestId'
GROUP BY p.Value
ORDER BY MAX(e.Timestamp) DESC");

            var eventIdsByRequest = await db.QueryAsync<int>(@"
SELECT
    e.Id
FROM Events e
INNER JOIN Properties p ON p.EventId = e.Id AND p.Name = 'RequestId' AND p.Value IN @requestIds
GROUP BY e.Id", new { requestIds });

            // Load these events and properties
            var eventResults = new Dictionary<int, Event>();
            var data = await db.QueryAsync<Event, Property, Event>("SELECT e.*, p.* FROM Properties p INNER JOIN Events e ON e.Id = p.EventId AND e.Id IN @Ids", (e, p) =>
            {
                if (!eventResults.TryGetValue(e.Id, out Event useEvent))
                {
                    e.Properties = new List<Property>();
                    eventResults.Add(e.Id, e);
                    useEvent = e;
                }

                useEvent.Properties.Add(p);

                return useEvent;
            }, param: new { Ids = eventIdsByRequest });

            var results = eventResults.Values
                .GroupBy(m => m.Properties.FirstOrDefault(p => p.Name == "RequestId")?.Value ?? "")
                .ToDictionary(m => m.Key);

            return Ok(results);
        }
    }
}
