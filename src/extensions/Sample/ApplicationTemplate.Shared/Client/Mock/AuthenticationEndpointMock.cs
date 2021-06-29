using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Serialization;
using Uno.Extensions;
using Uno.Extensions.Http;

namespace ApplicationTemplate.Client
{
    public class AuthenticationEndpointMock : IAuthenticationEndpoint
    {
        private readonly ISerializer _serializer;

        public AuthenticationEndpointMock(ISerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public async Task CreateAccount(CancellationToken ct, string email, string password)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        public async Task ResetPassword(CancellationToken ct, string email)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        public async Task<AuthenticationData> Login(CancellationToken ct, string email, string password)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));

            return CreateAuthenticationData();
        }

        public async Task<AuthenticationData> RefreshToken(CancellationToken ct, AuthenticationData unauthorizedToken)
        {
            if (unauthorizedToken is null)
            {
                throw new ArgumentNullException(nameof(unauthorizedToken));
            }

            await Task.Delay(TimeSpan.FromSeconds(2));

            return null;// CreateAuthenticationData(unauthorizedToken.AccessToken.Payload);
        }

        private AuthenticationData CreateAuthenticationData(AuthenticationToken token = null, TimeSpan? timeToLive = null)
        {
            return null;
            //var encodedJwt = CreateJsonWebToken(token, timeToLive);
            //var jwt = new JwtData<AuthenticationToken>(encodedJwt, _serializer);

            //return new AuthenticationData.Builder
            //{
            //    AccessToken = jwt,
            //    RefreshToken = Guid.NewGuid().ToStringInvariant(),
            //    Expiration = jwt.Payload.Expiration,
            //};
        }

        private string CreateJsonWebToken(AuthenticationToken token = null, TimeSpan? timeToLive = null)
        {
            return null;
            //const string header = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9"; // alg=HS256, type=JWT
            //const string signature = "QWqnPP8W6ymexz74P6quP-oG-wxr7vMGqrEL8y_tV6M"; // dummy stuff

            //var now = DateTimeOffset.Now;

            //token = (token ?? AuthenticationToken.Default)
            //    .WithExpiration(now + (timeToLive ?? TimeSpan.FromMinutes(10)))
            //    .WithIssuedAt(now);

            //string payload;
            //using (var stream = new MemoryStream())
            //{
            //    _serializer.WriteToStream(token, typeof(AuthenticationToken), stream, canDisposeStream: false);
            //    payload = Convert.ToBase64String(stream.ToArray());
            //}

            //return header + '.' + payload + '.' + signature;
        }
    }
}
