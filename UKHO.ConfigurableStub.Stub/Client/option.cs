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

namespace UKHO.ConfigurableStub.Stub.Client
{
    public class Option<T>
    {
        public static Option<T> None = new Option<T>();
        private readonly T value;

        private Option()
        {
            IsSome = false;
        }

        public Option(T value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            this.value = value;
            IsSome = true;
        }

        public bool IsSome { get; }
        public bool IsNone => !IsSome;
        public T Value => IsSome ? value : throw new InvalidOperationException("IsNone");

        public override string ToString()
        {
            if (IsNone)
                return $"{nameof(Option<T>)}<{typeof(T).FullName}>.None";
            return $"{nameof(Option<T>)}<{typeof(T).FullName}>{{{value.ToString()}}}";
        }
    }
}