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
        private readonly string localStubAddress = $"Https://localhost:{DefaultPortConfiguration.HttpsPort}";

        /// <summary>
        ///     Starts the stub and waits for it to be listening on a port.
        /// </summary>
        /// <param name="standardOutputTextWriter">A text writer that the stub standard output will be redirected to.</param>
        /// <exception cref="Exception"></exception>
        public void Start(TextWriter standardOutputTextWriter)
        {
            stubTask = Task.Run(() => StubStartup.StartStub(standardOutputTextWriter));
            try
            {
                using (var stubClient = new StubClient(localStubAddress))
                {
                    TimingHelper.WaitFor(() => stubClient.HealthCheck().Result);
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