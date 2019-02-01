using System.Collections.Generic;

namespace UKHO.ConfigurableStub.Models
{
    public interface IRequestRecord<T>
    {
        T RequestBody { get; set; }
        Dictionary<string, string> RequestHeaders { get; set; }
    }

    public class RequestRecord<T> : IRequestRecord<T>
    {
        public T RequestBody { get; set; }
        public Dictionary<string, string> RequestHeaders { get; set; }
    }
}