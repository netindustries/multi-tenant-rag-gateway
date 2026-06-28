using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NII.Models
{
    public class ClientRegistrationRequest
    {
        public string ClientName { get; set; }
        public string EmailAddress { get; set; }
        public string RawApiKeyToRegister { get; set; }
    }
}