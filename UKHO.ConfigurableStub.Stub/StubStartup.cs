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
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UKHO.ConfigurableStub.Stub
{
    /// <summary>
    ///     The StubStartup class contains the static methods to start the stub
    /// </summary>
    public static class StubStartup
    {
        /// <summary>
        ///     Start the stub with Kestrel on default ports for http and https
        ///     You can pass in a TextWriter to get the output
        /// </summary>
        /// <remarks>
        ///     This stub will not work if not run from an executable with a new csproj.
        /// </remarks>
        /// >
        public static void StartStub(TextWriter textWriter = null, int httpPort = DefaultPortConfiguration.HttpPort,
            int httpsPort = DefaultPortConfiguration.HttpsPort)
        {
            if (textWriter != null) Console.SetOut(textWriter);

            BuildStubWebHost(httpPort, httpsPort).Run();
        }

        private static IHost BuildStubWebHost(int httpPort, int httpsPort)
        {
            var certificateBuilder = new CertificateBuilder("password1", TimeSpan.FromMinutes(40));
            var cert = new X509Certificate2(certificateBuilder.CertificateStream.ToArray(), "password1");
            return Host.CreateDefaultBuilder(new string[0])
                .ConfigureWebHostDefaults(builder =>
                builder
                    .UseStartup<Startup>()
                    .ConfigureLogging((x, log) =>
                    {
                        log.SetMinimumLevel(LogLevel.Information);
                        log.AddConsole(lo => lo.IncludeScopes = true);
                    })
                    .UseKestrel(options =>
                    {
                        options.Listen(IPAddress.Loopback, httpsPort, listenOptions => { listenOptions.UseHttps(cert); });
                        options.Listen(IPAddress.Loopback, httpPort);
                    }))
                .Build();
        }
    }
}