namespace Uno.Extensions.Http;

/// <summary>
/// Implements a simple authentication token provider
/// </summary>
public class SimpleAuthenticationToken : IAuthenticationTokenProvider
{
	/// <inheritdoc />
	public string? AccessToken { get; set; }

	/// <inheritdoc />
	public Task<string> GetAccessToken(HttpRequestMessage? requestMessage = default, CancellationToken cancellationToken = default) => Task.FromResult(AccessToken ?? string.Empty);
}
