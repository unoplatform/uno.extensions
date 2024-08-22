using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Uno.Extensions.Authentication;

namespace Uno.Extensions.Http.Kiota
{

	public class KiotaAuthenticationAdapter : IAuthenticationProvider, IAccessTokenProvider
	{
		private readonly ITokenCache _tokenCache;

		/// <summary>
		/// Initializes a new instance of the <see cref="KiotaAuthenticationAdapter"/> class.
		/// </summary>
		/// <param name="tokenCache">The token cache used to store and retrieve tokens.</param>
		/// <param name="allowedHostsValidator">The validator used to verify if a host is allowed.</param>
		public KiotaAuthenticationAdapter(ITokenCache tokenCache, AllowedHostsValidator allowedHostsValidator)
		{
			_tokenCache = tokenCache;
			AllowedHostsValidator = allowedHostsValidator;
		}

		/// <summary>
		/// Gets the <see cref="AllowedHostsValidator"/> used to validate allowed hosts.
		/// </summary>
		public AllowedHostsValidator AllowedHostsValidator { get; }

		/// <summary>
		/// Asynchronously retrieves the authorization token for the specified URI.
		/// </summary>
		/// <param name="uri">The URI for which the token is requested.</param>
		/// <param name="additionalAuthenticationContext">Additional context for authentication, if any.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task that represents the asynchronous operation, returning the authorization token.</returns>
		/// <exception cref="InvalidOperationException">Thrown if the URI host is not allowed or if no access token is available.</exception>
		public async Task<string> GetAuthorizationTokenAsync(
			Uri? uri = null,
			Dictionary<string, object>? additionalAuthenticationContext = null,
			CancellationToken cancellationToken = default)
		{
			if (uri != null && !AllowedHostsValidator.IsUrlHostValid(uri))
			{
				throw new InvalidOperationException("The URI host is not allowed.");
			}

			var token = await _tokenCache.AccessTokenAsync(cancellationToken);

			if (string.IsNullOrEmpty(token))
			{
				throw new InvalidOperationException("No access token is available.");
			}

			return token;
		}

		/// <summary>
		/// Adds the authorization token to the request headers.
		/// </summary>
		/// <param name="request">The request to add the authorization header to.</param>
		/// <param name="additionalAuthenticationContext">Additional context for authentication, if any.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		public async Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null,
			CancellationToken cancellationToken = default)
		{
			var token = await GetAuthorizationTokenAsync(null, additionalAuthenticationContext, cancellationToken);
			if (!string.IsNullOrEmpty(token))
			{
				request.Headers.Add("Authorization", $"Bearer {token}");
			}
		}
	}
}
