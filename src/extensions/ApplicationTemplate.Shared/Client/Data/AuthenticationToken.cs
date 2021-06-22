using System;
using Uno.Extensions.Serialization;
using Uno;
using System.Text.Json.Serialization;

namespace ApplicationTemplate.Client
{
    public partial class AuthenticationToken
    {
        [JsonPropertyName("unique_name")]
        public string Email { get; }

        [JsonPropertyName("exp")]
        public DateTimeOffset Expiration { get; } = DateTimeOffset.MinValue;

        [JsonPropertyName("iat")]
        public DateTimeOffset IssuedAt { get; } = DateTimeOffset.MinValue;
    }
}
