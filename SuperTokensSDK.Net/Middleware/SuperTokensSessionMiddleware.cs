using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace SuperTokensSDK.Net.Middleware
{
    public delegate Task RequestDelegate(HttpContent context);
    public class SuperTokensSessionMiddleware
    {
        private readonly RequestDelegate _next;

        public SuperTokensSessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContent context, ISuperTokenService service)
        {
            // Реализация middleware
            await _next(context);
        }
    }
    public static class SuperTokensSessionMiddlewareExtensions
    {
        public static IApplicationBuilder UseSuperTokensMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SuperTokensSessionMiddleware>();
        }
    }
}