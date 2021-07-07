using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Uno;

namespace ApplicationTemplate.Client
{
    public partial class ChuckNorrisData
    {
        public string Id { get; set; }

        public string Value { get; set; }

        [JsonPropertyName("icon_url")]
        public string IconUrl { get; set; }

        public string[] Categories { get; set; }
    }
}
