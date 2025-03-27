using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperTokensSDK.Net.DataClasses
{
    public class SuperTokenSession
    {

        /// <summary>
        /// Уникальный идентификатор сессии
        /// </summary>
        public string? SessionHandle { get; set; }

        /// <summary>
        /// Access token (JWT)
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// Refresh token
        /// </summary>
        public string? RefreshToken { get; set; }

        /// <summary>
        /// ID пользователя
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Время истечения access token
        /// </summary>
        public DateTime AccessTokenExpiry { get; set; }

        /// <summary>
        /// Время истечения refresh token
        /// </summary>
        public DateTime RefreshTokenExpiry { get; set; }

    }
}