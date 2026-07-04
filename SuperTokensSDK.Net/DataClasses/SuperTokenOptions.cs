using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperTokensSDK.Net.DataClasses
{
    public class SuperTokenOptions
    {
        public string? AuthURI { get; set; }

        /// <summary>
        /// Base URI of the SuperTokens Core service (default: http://supertokens-core:3567).
        /// </summary>
        public string? CoreUri { get; set; } = "http://supertokens-core:3567";

        /// <summary>
        /// API key used to authenticate with SuperTokens Core.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Application name used by SuperTokens Core.
        /// </summary>
        public string? AppName { get; set; } = "HistoneDB";
    }
}