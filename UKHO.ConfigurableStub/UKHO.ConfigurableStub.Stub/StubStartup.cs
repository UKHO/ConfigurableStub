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
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace UKHO.ConfigurableStub.Stub
{
    /// <summary>
    /// The StubStartup class contains the static methods to start the stub
    /// </summary>
    public static class StubStartup
    {
        /// <summary>
        /// Will start the stub use default ports of 44310 for https and 44336 for http. The self signed cert it uses will expire after 40 minutes
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            BuildStubWebHost(44310, 44336, 40);
        }

        /// <summary>
        /// Start the stub with specified ports and a specified certificate expiry.
        /// </summary>
        /// <param name="httpsPort"></param>
        /// <param name="httpPort"></param>
        /// <param name="certExpiryInMinutes"></param>
        public static void StartWithSpecifiedPorts(int httpsPort, int httpPort, int certExpiryInMinutes = 40)
        {
            BuildStubWebHost( httpsPort,  httpPort,  certExpiryInMinutes).Run();
        }

        private static IWebHost BuildStubWebHost(int httpsPort, int httpPort, int certExpiryInMinutes)
        {
            var certificateBuilder = new CertificateBuilder("password1", TimeSpan.FromMinutes(certExpiryInMinutes));
            var cert = new X509Certificate2(certificateBuilder.CertificateStream.ToArray(), "password1");
            return WebHost.CreateDefaultBuilder(new string[0])
                .UseStartup<Startup>()
                .UseKestrel(options =>
                            {
                                options.Listen(IPAddress.Loopback, httpsPort, listenOptions => { listenOptions.UseHttps(cert); });
                                options.Listen(IPAddress.Loopback, httpPort);
                            })
                .Build();
        }
    }
}