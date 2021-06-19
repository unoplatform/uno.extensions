using System;
using MallardMessageHandlers;

namespace Uno.Extensions.Http
{
    public partial class AuthenticationData : IAuthenticationToken
    {
        public string RefreshToken { get; set; }

        public DateTimeOffset? Expiration { get; set; }

        public bool CanBeRefreshed => !string.IsNullOrEmpty(RefreshToken);

        public string AccessToken { get; set; }
    }
}
