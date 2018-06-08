using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cedita.Labs.Jrnal.Web.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public int InstanceId { get; set; }
        public void OnGet(int? instanceId)
        {
            if (instanceId.HasValue)
                InstanceId = instanceId.Value;
        }
    }
}
