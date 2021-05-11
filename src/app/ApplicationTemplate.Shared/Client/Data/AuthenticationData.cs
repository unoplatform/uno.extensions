using System;
using GeneratedSerializers;
using MallardMessageHandlers;
using Uno;

namespace ApplicationTemplate.Client
{
    [GeneratedImmutable]
    public partial class AuthenticationData : IAuthenticationToken
    {
        [EqualityHash]
        [SerializationProperty("access_token")]
        public JwtData<AuthenticationToken> AccessToken { get; }

        [SerializationProperty("refresh_token")]
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
