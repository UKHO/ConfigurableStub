// British Crown Copyright © 2020,
// All rights reserved.
// 
// You may not copy the Software, rent, lease, sub-license, loan, translate, merge, adapt, vary
// re-compile or modify the Software without written permission from UKHO.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
// OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT
// SHALL CROWN OR THE SECRETARY OF STATE FOR DEFENCE BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
// TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
// BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING
// IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
// OF SUCH DAMAGE.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

using UKHO.ConfigurableStub.Stub.Models;

namespace UKHO.ConfigurableStub.Stub
{
    internal static class StubRouter
    {
        private static ILogger logger;

        internal static IApplicationBuilder UseStubRouter(this IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger("StubRouter");
            var mappedRoutes =
                new ConcurrentDictionary<string, RouteConfiguration>();
            var lastRequests = new ConcurrentDictionary<string, IList<IRequestRecord<object>>>();

            var defaultRouteHandler = new RouteHandler(context =>
                                                       {
                                                           var verb = context.Request.Method.ToUpperInvariant();
                                                           var resource = ((string)context.GetRouteValue("resource")).TrimEnd('/');
                                                           var key = $"{verb}:api/{resource}";

                                                           StoreRequestInformation(context, key, lastRequests);

                                                           if (mappedRoutes.ContainsKey(key)) return SetMappedResponse(mappedRoutes, key, context);

                                                           context.Response.StatusCode = 500;
                                                           return context.Response.WriteAsync("NO ROUTE DEFINED!");
                                                       });

            var routeBuilder = new RouteBuilder(app, defaultRouteHandler);
            routeBuilder.MapRoute("Routes", "api/{*resource}");

            routeBuilder.MapGet("health", //get details from last request to url...
                                context =>
                                {
                                    context.Response.StatusCode = 200;
                                    return Task.CompletedTask;
                                });

            routeBuilder.MapGet("stub/{verb}/history/api/{*resource}", //get details from last request to url...
                                context =>
                                {
                                    var verb = context.GetRouteValue("verb").ToString().ToUpperInvariant();
                                    var resource = ((string)context.GetRouteValue("resource")).TrimEnd('/');

                                    var key = $"{verb}:api/{resource}";
                                    if (lastRequests.ContainsKey(key))
                                    {
                                        context.Response.StatusCode = 200;
                                        return context.Response.WriteAsync(JsonConvert.SerializeObject(lastRequests[key]));
                                    }

                                    context.Response.StatusCode = 404;
                                    return context.Response.WriteAsync($"couldn't find any recent requests for {key}");
                                });

            routeBuilder.MapGet("stub/{verb}/api/{*resource}", //get details from last request to url...
                                context =>
                                {
                                    var verb = context.GetRouteValue("verb").ToString().ToUpperInvariant();
                                    var resource = ((string)context.GetRouteValue("resource")).TrimEnd('/');

                                    var key = $"{verb}:api/{resource}";
                                    if (lastRequests.ContainsKey(key))
                                    {
                                        context.Response.StatusCode = 200;
                                        return context.Response.WriteAsync(JsonConvert.SerializeObject(lastRequests[key].First()));
                                    }

                                    context.Response.StatusCode = 404;
                                    return context.Response.WriteAsync($"couldn't find any recent requests for {key}");
                                });

            routeBuilder.MapPost("stub/{verb}/api/{*resource}", //Set expected response for a URL.
                                 context =>
                                 {
                                     var verb = context.GetRouteValue("verb").ToString().ToUpperInvariant();
                                     var resource = verb + ":api/" + ((string)context.GetRouteValue("resource")).TrimEnd('/');

                                     var reader = new StreamReader(context.Request.Body);
                                     var body = reader.ReadToEnd();

                                     mappedRoutes[resource] = JsonConvert.DeserializeObject<RouteConfiguration>(body);

                                     context.Response.StatusCode = 204; //NoContent
                                     return Task.CompletedTask;
                                 });

            routeBuilder.MapDelete("stub",
                                   context =>
                                   {
                                       mappedRoutes = new ConcurrentDictionary<string, RouteConfiguration>();
                                       lastRequests = new ConcurrentDictionary<string, IList<IRequestRecord<object>>>();
                                       context.Response.StatusCode = 204;
                                       return Task.CompletedTask;
                                   });

            var routes = routeBuilder.Build();
            app.UseRouter(routes);
            return app;
        }

        private static Task SetMappedResponse(ConcurrentDictionary<string, RouteConfiguration> mappedRoutes,
                                              string key,
                                              HttpContext context)
        {

            var mapping = mappedRoutes[key];
            context.Response.StatusCode = mapping.StatusCode;
            context.Response.ContentType = mapping.ContentType;
            if (mapping.LastModified != null)
            {
                context.Response.Headers[HeaderNames.LastModified] = new StringValues(mapping.LastModified.Value.ToString("R"));
            }
            if (mapping.RequiredHeaders != null)
            {
                var missingRequiredHeaders = mapping.RequiredHeaders
                    .Where(requiredHeader => !context.Request.Headers.ContainsKey(requiredHeader)).ToList();
                var missingHeaders = missingRequiredHeaders.Any();

                if (missingHeaders)
                {
                    context.Response.StatusCode = 500;
                    return context.Response.WriteAsync(
                                                       $"Missing required headers: {string.Join(", ", missingRequiredHeaders)}");
                }
            }

            if (string.IsNullOrEmpty(mappedRoutes[key].Base64EncodedBinaryResponse))
                return context.Response.WriteAsync(mapping.Response);

            using (var binaryResponseData =
                new MemoryStream(Convert.FromBase64String(mappedRoutes[key].Base64EncodedBinaryResponse)))
            {
                return binaryResponseData.CopyToAsync(context.Response.Body);
            }
        }

        private static void StoreRequestInformation(HttpContext context,
                                                    string key,
                                                    ConcurrentDictionary<string, IList<IRequestRecord<object>>> lastRequests)
        {
            object requestObject;
            try
            {
                requestObject = JsonConvert.DeserializeObject(new StreamReader(context.Request.Body).ReadToEnd());
            }
            catch (Exception e)
            {
                logger.LogInformation(new EventId(1234),
                                      e,
                                      "Non-fatal error on deserializing request as json. Request will be stored as a string instead.");
                requestObject = new StreamReader(context.Request.Body).ReadToEnd();
            }

            var requestHeaders = context.Request.Headers.ToDictionary(pair => pair.Key, pair => pair.Value.ToString());

            var requestRecord = new RequestRecord<object>
                                {
                                    RequestBody =
                                        requestObject,
                                    RequestHeaders =
                                        requestHeaders
                                };
            if (lastRequests.ContainsKey(key))
            {
                lastRequests[key].Add(requestRecord);
            }
            else
            {
                lastRequests[key] = new List<IRequestRecord<object>> { requestRecord };
            }
        }
    }
}