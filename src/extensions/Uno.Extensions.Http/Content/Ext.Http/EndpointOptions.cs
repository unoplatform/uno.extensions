using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Uno.Extensions.Http
{
    public class EndpointOptions
    {
        public string Url { get; set; }

        public Dictionary<string, bool> Features { get; set; }

        protected bool FeatureEnabled([CallerMemberName] string feature = null)
        {
            if (string.IsNullOrEmpty(feature))
            {
                return false;
            }
            return (Features is not null && Features.TryGetValue(feature, out var featureValue)) ? featureValue : false;
        }

        [JsonIgnore]
        public bool UseNativeHandler => FeatureEnabled();

        [JsonIgnore]
        public bool UseDefaultHeaders => FeatureEnabled();

        [JsonIgnore]
        public bool UseExceptionHubHandler => FeatureEnabled();

        [JsonIgnore]
        public bool UseNetworkExceptionHandler => FeatureEnabled();
    }
}
