using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace SuperTokensSDK.Net
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SuperTokenSessionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var superToken = context.HttpContext.RequestServices.GetRequiredService<ISuperTokenService>();
            var AccessToken = context.HttpContext.Request.Headers["Authorizaion"].ToString().Replace("Bearer ", "");
            if (!await SuperTokenSessionAttribute.IsAuthorized(AccessToken, superToken))
            {
                context.Result = new UnauthorizedResult();
            }
            
        }
        private static async Task<bool> IsAuthorized(string? AccessToken, ISuperTokenService superTokenService)
        {
            return !(string.IsNullOrWhiteSpace(AccessToken) || !(await superTokenService.ValidateSession(AccessToken)).IsValid);
        }
    }
}