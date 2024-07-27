namespace Uno.Extensions.Authentication;

/// <summary>
/// A service with methods for authenticating a user.
/// </summary>
public interface IAuthenticationService
{
	/// <summary>
	/// Gets the names of the authentication providers that are supported by this service.
	/// </summary>
	string[] Providers { get; }

	/// <summary>
	/// Logs in the user with the specified credentials using the specified provider name.
	/// </summary>
	/// <param name="dispatcher">
	/// A dispatcher that can be used to perform operations on the UI thread.
	/// </param>
	/// <param name="credentials">
	/// A dictionary of credentials to use for logging in the user. Optional
	/// </param>
	/// <param name="provider">
	/// The name of the authentication provider to use. Optional
	/// </param>
	/// <param name="cancellationToken">
	/// A cancellation token that can be used to cancel the login operation. Optional
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous login operation. The task result is true if the login was successful.
	/// </returns>
	ValueTask<bool> LoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials = default, string? provider = null, CancellationToken? cancellationToken = default);

	/// <summary>
	/// Refreshes the authentication tokens for the current user.
	/// </summary>
	/// <param name="cancellationToken">
	/// A cancellation token that can be used to cancel the refresh operation. Optional
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous refresh operation. The task result is true if the refresh was successful.
	/// </returns>
	ValueTask<bool> RefreshAsync(CancellationToken? cancellationToken = default);

	/// <summary>
	/// Logs out the current user.
	/// </summary>
	/// <param name="dispatcher">
	/// A dispatcher that can be used to perform operations on the UI thread.
	/// </param>
	/// <param name="cancellationToken">
	/// A cancellation token that can be used to cancel the logout operation. Optional
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous logout operation. The task result is true if the logout was successful.
	/// </returns>
	ValueTask<bool> LogoutAsync(IDispatcher? dispatcher, CancellationToken? cancellationToken = default);

	/// <summary>
	/// Gets a value indicating whether the current user is authenticated.
	/// </summary>
	/// <param name="cancellationToken">
	/// A cancellation token that can be used to cancel the operation. Optional
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result is true if the user is authenticated.
	/// </returns>
	ValueTask<bool> IsAuthenticated(CancellationToken? cancellationToken = default);

	/// <summary>
	/// Gets claims associated with a specified token type.
	/// </summary>
	/// <param name="tokenType">
	/// The token type <see cref="TokenCacheExtensions.IdTokenKey"/> or <see cref="TokenCacheExtensions.AccessTokenKey"/></param>
	/// <param name="cancellationToken">
	/// A cancellation token that can be used to cancel the operation. Optional
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result is a an enumerable of claims associated with the specified token.
	/// </returns>
	ValueTask<IEnumerable<Claim>> GetClaims(string tokenType = TokenCacheExtensions.IdTokenKey, CancellationToken? cancellationToken = default);

	/// <summary>
	/// Defines an event that is raised when the user is logged out.
	/// </summary>
	event EventHandler LoggedOut;
}
