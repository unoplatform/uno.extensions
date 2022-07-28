namespace Uno.Extensions.Authentication;

public static class AuthenticationServiceExtensions
{
	public static ValueTask<bool> LoginAsync (this IAuthenticationService auth, IDictionary<string, string>? credentials = default, string? provider = null, CancellationToken? cancellationToken = default)
	{
		return auth.LoginAsync(default, credentials, provider, cancellationToken);
	}
	public static ValueTask<bool> LogoutAsync(this IAuthenticationService auth, CancellationToken? cancellationToken = default)
	{
		return auth.LogoutAsync(default, cancellationToken);
	}
}
