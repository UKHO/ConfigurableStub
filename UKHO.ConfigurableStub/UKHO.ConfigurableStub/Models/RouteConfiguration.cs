using System.Collections.Generic;

namespace UKHO.ConfigurableStub.Models
{
    internal class RouteConfiguration
    {
        public int StatusCode { get; set; }
        public string Thumbprint { get; set; }
        public IEnumerable<string> RequiredHeaders { get; set; }
        public string Response { get; set; }
        public string Base64EncodedBinaryResponse { get; set; }
        public string ContentType { get; set; }
    }
}