namespace Uno.Extensions.Authentication;

/// <summary>
/// Extension methods for <see cref="IAuthenticationService"/>.
/// </summary>
public static class AuthenticationServiceExtensions
{
	/// <summary>
	/// Logs in the user using the specified credentials.
	/// </summary>
	/// <param name="auth">
	/// The <see cref="IAuthenticationService"/> to use.
	/// </param>
	/// <param name="credentials">
	/// The credentials to use for the login. Optional
	/// </param>
	/// <param name="provider">
	/// The authentication provider name to specify for login. Optional
	/// </param>
	/// <param name="cancellationToken">
	/// A <see cref="CancellationToken"/> used to cancel the login operation. Optional
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask"/> that completes when the login operation is complete.
	/// </returns>
	public static ValueTask<bool> LoginAsync(this IAuthenticationService auth, IDictionary<string, string>? credentials = default, string? provider = null, CancellationToken? cancellationToken = default)
	{
		return auth.LoginAsync(default, credentials, provider, cancellationToken);
	}

	/// <summary>
	/// Logs out the user.
	/// </summary>
	/// <param name="auth">
	/// The <see cref="IAuthenticationService"/> to use.
	/// </param>
	/// <param name="cancellationToken">
	/// A <see cref="CancellationToken"/> used to cancel the logout operation. Optional
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask"/> that completes when the logout operation is complete.
	/// </returns>
	public static ValueTask<bool> LogoutAsync(this IAuthenticationService auth, CancellationToken? cancellationToken = default)
	{
		return auth.LogoutAsync(default, cancellationToken);
	}
}
