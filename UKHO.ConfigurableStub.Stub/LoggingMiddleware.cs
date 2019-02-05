// British Crown Copyright © 2018,
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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace UKHO.ConfigurableStub.Stub
{
    [ExcludeFromCodeCoverage] // extension methods that call extension methods make this difficult to test.
    internal static class LoggingMiddleware
    {
        internal static IApplicationBuilder UseErrorLogging(this IApplicationBuilder appBuilder, ILoggerFactory loggerFactory)
        {
            return appBuilder.Use(async (context, func) =>
                                  {
                                      try
                                      {
                                          await func();
                                      }
                                      catch (Exception e)
                                      {
                                          loggerFactory
                                              .CreateLogger(context.Request.Path)
                                              .LogError(1, e, "{Exception}", e);
                                          context.Response.StatusCode = 500;
                                      }
                                  });
        }

        internal static IApplicationBuilder UseLogAllRequests(this IApplicationBuilder appBuilder, ILoggerFactory loggerFactory)
        {
            return appBuilder.Use(async (context, func) =>
                                  {
                                      var logger = loggerFactory
                                          .CreateLogger(typeof(LoggingMiddleware).FullName);

                                      using (var requestBodyStream = new MemoryStream())
                                      {
                                          var originalRequestBody = context.Request.Body;
                                          await originalRequestBody.CopyToAsync(requestBodyStream);
                                          context.Request.Body = requestBodyStream;

                                          var requestBodyText = await ReadAndResetStream(requestBodyStream);

                                          var url = context.Request.GetDisplayUrl();
                                          var headerDictionary = context.Request.Headers.Where(kv => kv.Key != "X-ARR-ClientCert").ToDictionary(kv => kv.Key, kv => kv.Value);

                                          var originalResponseBody = context.Response.Body;
                                          using (var responseBody = new MemoryStream())
                                          {
                                              context.Response.Body = responseBody;

                                              try
                                              {
                                                  await func();
                                              }
                                              finally
                                              {
                                                  context.Request.Body = originalRequestBody;
                                                  context.Response.Body = originalResponseBody;
                                                  responseBody.Seek(0, SeekOrigin.Begin);
                                                  if (responseBody.Length > 0)
                                                      await responseBody.CopyToAsync(originalResponseBody);

                                                  var bodyAsString = await ReadResponseBodyAsString(context, responseBody);
                                                  logger.LogInformation(0,
                                                                        "Request Method: {requestMethod}, Request Url: {url}, Request Header:{headerDictionary}, Request Body: {requestBodyText}, Response Code: {responseCode}, Response Content Length: {responseContentLength}, Response Content Type: {responseContentType}, Response Body: {responseBody}",
                                                                        context.Request.Method,
                                                                        url,
                                                                        headerDictionary,
                                                                        requestBodyText,
                                                                        context.Response.StatusCode,
                                                                        responseBody.Length,
                                                                        context.Response.ContentType,
                                                                        bodyAsString);
                                              }
                                          }
                                      }
                                  });
        }

        private static async Task<string> ReadResponseBodyAsString(HttpContext context, Stream responseBody)
        {
            string bodyAsString;
            if (context.Response.ContentLength == 0)
                bodyAsString = null;
            else
            {
                bodyAsString = await ReadAndResetStream(responseBody);
                if (context.Response.ContentType?.IndexOf("json", StringComparison.InvariantCultureIgnoreCase) < 0
                    && !string.IsNullOrEmpty(bodyAsString))
                {
                    bodyAsString = "Redacted as its not JSON.";
                }
            }

            return bodyAsString;
        }

        private static async Task<string> ReadAndResetStream(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            try
            {
                return await new StreamReader(stream).ReadToEndAsync();
            }
            finally
            {
                stream.Seek(0, SeekOrigin.Begin);
            }
        }
    }
}