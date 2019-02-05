using System.Collections.Generic;

namespace UKHO.ConfigurableStub.Stub.Models
{
    /// <summary>
    /// The type used by the stub to represent request that have happened.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRequestRecord<T>
    {
        /// <summary>
        /// The body of the request.
        /// </summary>
        T RequestBody { get; set; }
        /// <summary>
        /// The header for the request
        /// </summary>
        Dictionary<string, string> RequestHeaders { get; set; }
    }

    /// <summary>
    /// The type used by the stub to represent request that have happened.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RequestRecord<T> : IRequestRecord<T>
    {
        /// <summary>
        /// The body of the request
        /// </summary>
        public T RequestBody { get; set; }
        /// <summary>
        /// The request headers
        /// </summary>
        public Dictionary<string, string> RequestHeaders { get; set; }
    }
}