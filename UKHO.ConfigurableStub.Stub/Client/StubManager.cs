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
using System.Threading.Tasks;

using UKHO.ConfigurableStub.Stub.Client.Helpers;

namespace UKHO.ConfigurableStub.Stub.Client
{
    /// <summary>
    ///     A class to help manage starting up the UKHO.ConfigurableStub.
    /// </summary>
    public class StubManager : IDisposable
    {
        private Task stubTask;
        /// <summary>
        /// The local address for the stub.
        /// </summary>
        public string LocalStubAddress { get; private set; }

        /// <summary>
        ///     Starts the stub and waits for it to be listening on specified ports.
        /// </summary>
        /// <param name="standardOutputTextWriter">A text writer that the stub standard output will be redirected to.</param>
        /// <param name="httpPort">The http port to start the stub on</param>
        /// <param name="httpsPort">The https port to start the stub on</param>
        /// <param name="timeout">Timeout on waiting for the stub to start</param>
        /// <exception cref="Exception"></exception>
        public void Start(TextWriter standardOutputTextWriter,int httpPort = DefaultPortConfiguration.HttpPort, int httpsPort = DefaultPortConfiguration.HttpsPort, int timeoutSeconds = 5)
        {
            LocalStubAddress = $"Https://localhost:{httpsPort}";
            stubTask = Task.Run(() => StubStartup.StartStub(standardOutputTextWriter, httpPort, httpsPort));
            try
            {
                using (var stubClient = new StubClient(LocalStubAddress))
                {
                    // ReSharper disable once AccessToDisposedClosure - this closure is only used in the timing helper and will not be used outside the using.
                    TimingHelper.WaitFor(() => stubClient.HealthCheck().Result, timeoutSeconds);
                }
            }
            catch
            {
                throw new Exception("Stub did not start correctly.");
            }
        }

        /// <summary>
        ///     A dispose for the stub manager.
        /// </summary>
        public void Dispose()
        {
            stubTask?.Dispose();
        }
    }
}