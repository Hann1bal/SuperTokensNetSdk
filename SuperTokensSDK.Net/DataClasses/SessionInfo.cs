using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperTokensSDK.Net.DataClasses
{
    public class SessionInfo
    {
        public string UserId { get; set; } = "";

        public string? SessionHandle { get; set; }

        public DateTime AccessTokenExpiry { get; set; }

        public Dictionary<string, object> Claims { get; set; } = new Dictionary<string, object>();
    }
}