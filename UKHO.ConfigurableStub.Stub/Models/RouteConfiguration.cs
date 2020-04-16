using System;
using System.Collections.Generic;

namespace UKHO.ConfigurableStub.Stub.Models
{
    /// <summary>
    /// The type used by the stub to configure responses to certain routes.
    /// </summary>
    public class RouteConfiguration
    {
        /// <summary>
        /// The status code you wish to be returned
        /// </summary>
        public int StatusCode { get; set; }
        /// <summary>
        /// Headers that are required to be present
        /// </summary>
        public IEnumerable<string> RequiredHeaders { get; set; }
        /// <summary>
        /// The response string you wish to be returned if there is no configured binary return
        /// </summary>
        public string Response { get; set; }
        /// <summary>
        /// The Base64 encoded binary you wish to return this will prevent the returning of the response string
        /// </summary>
        public string Base64EncodedBinaryResponse { get; set; }
        /// <summary>
        /// The content type you wish to return e.g application/json, application/zip etc.
        /// </summary>
        public string ContentType { get; set; }
        
        public DateTimeOffset? LastModified { get; set; }
    }
}