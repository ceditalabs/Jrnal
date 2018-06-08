using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Cedita.Labs.Jrnal.Db;
using Cedita.Labs.Jrnal.Db.Models;
using Cedita.Labs.Jrnal.Models.Containers;
using Dapper;
using Microsoft.AspNetCore.Mvc;

namespace Cedita.Labs.Jrnal.Controllers.Web
{
    [Route("api/web/[controller]")]
    [ApiController]
    public class ApplicationsController : ControllerBase
    {
        private readonly IDbConnection db;
        public ApplicationsController(IDbConnection db)
        {
            this.db = db;
        }

        /// <summary>
        /// Exposes a list of applications that can be used to filter logs
        /// </summary>
        /// <returns></returns>
        [HttpGet("List")]
        public async Task<IActionResult> ListApplications()
        {
            return Ok(await db.QueryAsync<Application>("SELECT Id, Name FROM Applications"));
        }
    }
}
