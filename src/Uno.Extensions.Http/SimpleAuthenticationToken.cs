using System.Threading.Tasks;

namespace Uno.Extensions.Http;

public class SimpleAuthenticationToken : IAuthenticationToken
{
	public string? AccessToken { get; set; }

	public Task<string> GetAccessToken() => Task.FromResult(AccessToken ?? string.Empty);
}
