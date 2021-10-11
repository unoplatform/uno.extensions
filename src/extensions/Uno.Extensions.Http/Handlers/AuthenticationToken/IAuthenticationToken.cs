using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Extensions.Http.Handlers
{
    public interface IAuthenticationToken
    {
        /// <summary>
        /// Returns the access token.
        /// </summary>
        string AccessToken { get; }

        /// <summary>
        /// Determines whether or not the token can be refreshed.
        /// This is usually indicated by the fact that there is a refresh token.
        /// </summary>
        bool CanBeRefreshed { get; }
    }
}
