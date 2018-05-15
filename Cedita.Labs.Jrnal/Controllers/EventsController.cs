using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cedita.Labs.Jrnal.Db;
using Cedita.Labs.Jrnal.Db.Models;
using Cedita.Labs.Jrnal.Models.Containers;
using Chic.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Cedita.Labs.Jrnal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IRepository<Application> applicationRepository;
        private readonly ChildAotInsertionService insertionService;
        public EventsController(IRepository<Application> applicationRepository,
            ChildAotInsertionService insertionService)
        {
            this.applicationRepository = applicationRepository;
            this.insertionService = insertionService;
        }

        // GET api/values
        [HttpPost("raw")]
        public async Task<IActionResult> Post([FromBody]BulkEvents events)
        {
            var apiKey = Request.Headers["X-Seq-ApiKey"].FirstOrDefault() ?? String.Empty;
            var application = (await applicationRepository.GetByWhereAsync("ApiKey = @apiKey", new { apiKey })).FirstOrDefault();
            if (application == null) return Unauthorized();
            // Construct the tree of items
            var dbEvents = new List<Event>();
            foreach(var apiEvent in events.Events)
            {
                var dbEvent = new Db.Models.Event
                {
                    ApplicationId = application.Id,
                    HasException = apiEvent.Exception != null,
                    Level = apiEvent.Level,
                    MessageTemplate = apiEvent.MessageTemplate,
                    Properties = new List<Property>(),
                    RenderingGroups = new List<RenderingGroup>(),
                    Timestamp = apiEvent.Timestamp
                };

                if (apiEvent.Properties != null)
                {
                    foreach (var apiProperty in apiEvent.Properties)
                    {
                        dbEvent.Properties.Add(MapProperty(apiProperty.Key, apiProperty.Value));
                    }
                }
                if (apiEvent.Renderings != null)
                {
                    foreach (var apiRendering in apiEvent.Renderings)
                    {
                        dbEvent.RenderingGroups.Add(new RenderingGroup
                        {
                            Name = apiRendering.Key,
                            Renderings = apiRendering.Value.Select(m => new Db.Models.SingleRendering
                            {
                                Format = m.Format,
                                Rendering = m.Rendering
                            }).ToList()
                        });
                    }
                }

                dbEvents.Add(dbEvent);
            }

            await insertionService.InsertWithChildrenAsync(dbEvents);
            
            return Ok();
        }

        private Db.Models.Property MapProperty(string name, object val)
        {
            var dbProperty = new Db.Models.Property
            {
                Name = name,
                Children = new List<Db.Models.Property>()
            };
            if (val is string)
            {
                dbProperty.Value = val as string;
            }
            else if (val is JObject)
            {
                var jObject = val as JObject;
                foreach (var prop in jObject)
                {
                    dbProperty.Children.Add(MapProperty(prop.Key, prop.Value));
                }
            }
            else
            {
                dbProperty.Value = Newtonsoft.Json.JsonConvert.SerializeObject(val);
            }

            return dbProperty;
        }
    }
}
