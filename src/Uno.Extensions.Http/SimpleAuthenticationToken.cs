namespace Uno.Extensions.Http;

public class SimpleAuthenticationToken : IAuthenticationTokenProvider
{
	public string? AccessToken { get; set; }

	public Task<string> GetAccessToken() => Task.FromResult(AccessToken ?? string.Empty);
}
