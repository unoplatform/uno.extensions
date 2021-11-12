using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Uno.Extensions.Http
{
    public class EndpointOptions
    {
        public string? Url { get; set; }

        public bool UseNativeHandler { get; set; }
    }
}
