using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Uno;

namespace ApplicationTemplate.Client
{
    public partial class ChuckNorrisData
    {
        public string Id { get; }

        public string Value { get; }

        [JsonPropertyName("icon_url")]
        public string IconUrl { get; }

        public string[] Categories { get; }
    }
}
