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

        /// <summary>
        /// Simple extraction of logs from a given timestamp. This is used as a recurring API Call, the most recent log entry is returned
        /// as the "pointer", after which a subsequent api call can be made providing this same value to return any other log entries
        /// since it was last queried resulting in a linux tail effect.
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="category"></param>
        /// <param name="from"></param>
        /// <returns></returns>
        [HttpGet("FromTime")]
        public async Task<IActionResult> GetLogsFromTime(int appId, Models.EventTypeCategory category, DateTimeOffset? from = null)
        {
            if (!from.HasValue)
                from = DateTimeOffset.Now.AddHours(-6);

            string sql = $"SELECT e.*, p.* FROM Events e LEFT JOIN Properties p ON p.EventId = e.Id WHERE e.ApplicationId = @AppId AND e.Timestamp > @FromTime ";
            switch (category)
            {
                case Models.EventTypeCategory.Error:
                    sql += "AND e.Level = 'Error'";
                    break;
                case Models.EventTypeCategory.Mail:
                    sql += "AND ((p.Name = 'SourceContext' AND p.Value LIKE '%Communication%') or e.MessageTemplate LIKE '[[]MAIL%')";
                    break;
            }
            sql += " ORDER BY e.Id ASC";

            var eventResults = new Dictionary<int, Event>();
            var data = await db.QueryAsync<Event, Property, Event>(sql, (e, p) =>
            {
                if (!eventResults.TryGetValue(e.Id, out Event useEvent))
                {
                    e.Properties = new List<Property>();
                    eventResults.Add(e.Id, e);
                    useEvent = e;
                }

                useEvent.Properties.Add(p);

                return useEvent;
            }, new { AppId = appId, FromTime = from });

            var logResult = new LogResult();

            if (data == null || data.Count() == 0)
                return Ok(logResult);

            logResult.Pointer = eventResults.LastOrDefault().Value?.Timestamp;
            logResult.Logs = ParseLogs(eventResults);

            return Ok(logResult);
        }

        /// <summary>
        /// Provides an interface to search through logs. This will currently search all logs within a given date range (or last 24 hours if no date provided)
        /// and processes the message templates in code rather than SQL (To enable you to search event properties).
        /// 
        /// At a later date this will want to be refactored to be more performant and implement a tokenised search system (E.g. `failed p:SourceContext:Cedita.Payroll.PayrollCalculator` `[freetext on messagetemplate] [p for property]:[property name]:[property value]`)
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet("Search")]
        public async Task<IActionResult> SearchLogs(int appId, DateTime? from = null, DateTime? to = null, string filter = "")
        {
            // Convert the from / to values to DateTimeOffsets
            if (!from.HasValue)
                from = DateTime.Now.AddHours(-24);

            DateTimeOffset fromOffset = from.Value;
            DateTimeOffset? toOffset = to;

            // We need to extract all log entries within the date range provided. Message filtering can only occur once we have extracted our processed logs
            string sql = $"SELECT e.*, p.* FROM Events e LEFT JOIN Properties p ON p.EventId = e.Id WHERE e.ApplicationId = @AppId AND e.Timestamp >= @FromTime ";
            if (toOffset.HasValue)
                sql += " AND e.Timestamp <= @ToTime ";
            //if (!string.IsNullOrWhiteSpace(filter))
            //    sql += " AND (e.MessageTemplate LIKE @Filter OR p.Value LIKE @Filter OR e.Level LIKE @Filter) ";
            sql += " ORDER BY e.Id ASC";

            var eventResults = new Dictionary<int, Event>();
            var data = await db.QueryAsync<Event, Property, Event>(sql, (e, p) =>
            {
                if (!eventResults.TryGetValue(e.Id, out Event useEvent))
                {
                    e.Properties = new List<Property>();
                    eventResults.Add(e.Id, e);
                    useEvent = e;
                }

                useEvent.Properties.Add(p);

                return useEvent;
            }, new { AppId = appId, FromTime = fromOffset, ToTime = toOffset, Filter = filter });

            var logResult = new LogResult();
            if (data == null || data.Count() == 0)
                return Ok(logResult);

            var preProcessedLogs = ParseLogs(eventResults);
            logResult.Pointer = eventResults.LastOrDefault().Value?.Timestamp;
            if (string.IsNullOrWhiteSpace(filter))
            {
                logResult.Logs = preProcessedLogs;
                return Ok(logResult);
            }

            List<(int, DateTimeOffset, string, string)> filteredLogs = new List<(int, DateTimeOffset, string, string)>();
            foreach (var log in preProcessedLogs)
            {
                if (log.Item3.Contains(filter) || log.Item4 == filter)
                    filteredLogs.Add(log);

                // TODO Regex match
            }
            logResult.Logs = filteredLogs;

            return Ok(logResult);
        }

        /// <summary>
        /// Parses logs into a common format
        /// </summary>
        /// <param name="logs"></param>
        /// <returns></returns>
        protected IEnumerable<(int, DateTimeOffset, string, string)> ParseLogs(Dictionary<int, Event> logs)
        {
            return logs.Select(m =>
            {
                var template = m.Value.MessageTemplate;
                foreach (var p in m.Value.Properties)
                {
                    if (p == null)
                        continue;

                    template = template.Replace("{" + p.Name + "}", p.Value);
                }
                return (m.Value.Id, m.Value.Timestamp, template, m.Value.Level);
            });
        }

        public class LogResult
        {
            public IEnumerable<(int, DateTimeOffset, string, string)> Logs { get; set; }
            public DateTimeOffset? Pointer { get; set; }
        }
    }
}
