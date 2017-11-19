﻿using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AspectCore.APM.HttpProfiler;
using AspectCore.APM.Profiler;
using AspectCore.Injector;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace AspectCore.APM.AspNetCore
{
    public class HttpProfilingMiddleware
    {
        private readonly RequestDelegate _next;

        public HttpProfilingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var callbacks = httpContext.RequestServices.ResolveMany<IProfilingCallback<HttpProfilingCallbackContext>>();
            if (!callbacks.Any())
            {
                await _next(httpContext);
                return;
            }
            Stopwatch stopwatch = Stopwatch.StartNew();
            await _next(httpContext);
            stopwatch.Stop();
            var callbackContext = new HttpProfilingCallbackContext
            {
                Elapsed = stopwatch.ElapsedMilliseconds,
                HttpHost = httpContext.Request.Host.Host,
                HttpMethod = httpContext.Request.Method,
                HttpPath = httpContext.Request.Path,
                HttpPort = httpContext.Request.Host.Port.ToString(),
                HttpProtocol = httpContext.Request.Protocol,
                HttpScheme = httpContext.Request.Scheme,
                IdentityAuthenticationType = httpContext.User.Identity.AuthenticationType,
                IdentityName = httpContext.User.Identity.Name,
                RequestContentType = httpContext.Request.ContentType,
                ResponseContentType = httpContext.Response.ContentType,
                StatusCode = httpContext.Response.StatusCode.ToString(),
            };
            foreach (var callback in callbacks)
                await callback.Invoke(callbackContext);
        }
    }
}