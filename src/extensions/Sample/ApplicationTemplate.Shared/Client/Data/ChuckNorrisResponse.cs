using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Uno;

namespace ApplicationTemplate.Client
{
    public partial class ChuckNorrisResponse
    {
        [JsonPropertyName("result")]
        public ChuckNorrisData[] Quotes { get; set; }
    }
}
