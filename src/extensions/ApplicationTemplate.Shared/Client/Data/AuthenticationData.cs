using System;
using Uno.Extensions.Serialization;
using Uno.Extensions.Http.Handlers;
using Uno;
using System.Text.Json.Serialization;

namespace ApplicationTemplate.Client
{
    public partial class AuthenticationData : IAuthenticationToken
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; }

        public DateTimeOffset? Expiration { get; }

        [EqualityKey]
        public string Email => AccessToken?.Payload?.Email;

        [SerializationIgnore]
        public bool CanBeRefreshed => !string.IsNullOrEmpty(RefreshToken);

        [SerializationIgnore]
        string IAuthenticationToken.AccessToken => AccessToken?.Token;
    }
}
