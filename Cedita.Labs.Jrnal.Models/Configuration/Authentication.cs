using System.Collections.Generic;

namespace Cedita.Labs.Jrnal.Models.Configuration
{
    public class Authentication
    {
        public string OpenIdAuthority { get; set; }
        public string OpenIdClientId { get; set; }
        public string OpenIdClientSecret { get; set; }
        public List<string> AllowedSids { get; set; }
    }
}
