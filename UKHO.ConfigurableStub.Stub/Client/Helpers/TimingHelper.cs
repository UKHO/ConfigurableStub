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
using System.Threading;

namespace UKHO.ConfigurableStub.Stub.Client.Helpers
{
    internal static class TimingHelper
    {
        private static readonly TimeSpan OneHundredMilliseconds = TimeSpan.FromMilliseconds(100);

        public static bool WaitFor(Func<bool> predicate, double timeoutSeconds = 5.0, bool throwExceptionIfPredicateNotTrue = true)
        {
            var endTime = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            Exception lastException = null;
            bool predicateResult = false;

            do
            {
                try
                {
                    predicateResult = predicate();
                }
                catch (Exception excepton)
                {
                    lastException = excepton;
                }

                Thread.Sleep(OneHundredMilliseconds);
            }
            while (!predicateResult && DateTime.UtcNow < endTime);

            // Throw exception if predicate not true (and required), so as better identify problem; rather than muddying waters with null ref exceptions.
            if (throwExceptionIfPredicateNotTrue && !predicateResult)
            {
                if (lastException != null)
                    throw new Exception($"TimingHelper.WaitFor Predicate failed: The last exception thrown was : {lastException}");

                throw new Exception("TimingHelper.WaitFor Predicate failed");
            }

            return predicateResult;
        }
    }
}