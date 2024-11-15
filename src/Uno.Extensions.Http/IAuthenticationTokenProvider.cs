namespace Uno.Extensions.Http;

/// <summary>
/// Provider for authentication token
/// </summary>
public  interface IAuthenticationTokenProvider
{
	/// <summary>
	/// Retrieves the current access token
	/// </summary>
	/// <returns>Access token</returns>
	Task<string> GetAccessToken(HttpRequestMessage? requestMessage = default, CancellationToken cancellationToken = default);
}
