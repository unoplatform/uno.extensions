
namespace Uno.Extensions.Authentication;

/// <summary>
/// Base type for authentication providers.
/// </summary>
/// <param name="Logger">
/// The logger to use for recording messages produced during various authentication operations on this provider.
/// </param>
/// <param name="Name">
/// The name of this provider.
/// </param>
/// <param name="Tokens">
/// The token cache to use for the default behavior of getting any existing tokens.
/// </param>
public abstract record BaseAuthenticationProvider(ILogger Logger, string Name, ITokenCache Tokens) : IAuthenticationProvider
{
	/// <summary>
	/// Logs in the user using the specified credentials.
	/// </summary>
	/// <param name="dispatcher">
	/// The <see cref="IDispatcher"/> to use for dispatching any UI operations.
	/// </param>
	/// <param name="credentials">
	/// The credentials to use for the login.
	/// </param>
	/// <param name="cancellationToken">
	/// A <see cref="CancellationToken"/> used to cancel the login operation.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask"/> that completes when the login operation is complete.
	/// </returns>
	public async ValueTask<IDictionary<string, string>?> LoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		try
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Started");
			return await InternalLoginAsync(dispatcher, credentials, cancellationToken);
		}
		catch (Exception ex)
		{
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Error attempting to login [Error - {ex.Message}]");
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Login credentials {credentials?.ToString()}");

			// Exception is bubbled so that the caller of IAuthenticationService.LoginAsync can handle it
			throw;
		}
		finally
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"End");
		}
	}

	/// <summary>
	/// Logs in the user using the specified credentials.
	/// </summary>
	/// <param name="dispatcher">
	/// The <see cref="IDispatcher"/> to use for dispatching any UI operations.
	/// </param>
	/// <param name="credentials">
	/// The credentials to use for the login.
	/// </param>
	/// <param name="cancellationToken">
	/// A <see cref="CancellationToken"/> used to cancel the login operation.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask"/> that completes when the login operation is complete.
	/// </returns>
	protected virtual ValueTask<IDictionary<string, string>?> InternalLoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		// Default behavior is to return null, which indicates unsuccessful login 
		return default;
	}

	/// <summary>
	/// Logs out the user.
	/// </summary>
	/// <param name="dispatcher">
	/// The <see cref="IDispatcher"/> to use for dispatching any UI operations.
	/// </param>
	/// <param name="cancellationToken">
	/// A <see cref="CancellationToken"/> used to cancel the logout operation.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask"/> that completes once the user is logged out.
	/// </returns>
	public async ValueTask<bool> LogoutAsync(IDispatcher? dispatcher, CancellationToken cancellationToken)
	{
		try
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Started");
			return await InternalLogoutAsync(dispatcher, cancellationToken);
		}
		catch (Exception ex)
		{
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Error attempting to logout [Error - {ex.Message}]");

			// Exception is bubbled so that the caller of IAuthenticationService.LogoutAsync can handle it
			throw;
		}
		finally
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"End");
		}
	}

	/// <summary>
	/// Logs out the user.
	/// </summary>
	/// <param name="dispatcher">
	/// The <see cref="IDispatcher"/> to use for dispatching any UI operations.
	/// </param>
	/// <param name="cancellationToken">
	/// A <see cref="CancellationToken"/> used to cancel the logout operation.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask"/> that completes once the user is logged out.
	/// </returns>
	protected virtual async ValueTask<bool> InternalLogoutAsync(IDispatcher? dispatcher, CancellationToken cancellationToken)
	{
		// Default implementation is to return true, which will cause the token cache to be flushed
		return true;
	}

	/// <summary>
	/// Refreshes the authentication tokens.
	/// </summary>
	/// <param name="cancellationToken">
	/// A <see cref="CancellationToken"/> used to cancel the refresh operation.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask"/> that completes once the tokens have been refreshed.
	/// </returns>
	public async ValueTask<IDictionary<string, string>?> RefreshAsync(CancellationToken cancellationToken)
	{
		var tokens = await Tokens.GetAsync(cancellationToken);
		try
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Started");
			return await InternalRefreshAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Error attempting to refresh [Error - {ex.Message}]");
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Current tokens {tokens}");

			// Exception is bubbled so that the caller of IAuthenticationService.RefreshAsync can handle it
			throw;
		}
		finally
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"End");
		}
	}

	/// <summary>
	/// Refreshes the authentication tokens.
	/// </summary>
	/// <param name="cancellationToken">
	/// A <see cref="CancellationToken"/> used to cancel the refresh operation.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask"/> that completes once the tokens have been refreshed.
	/// </returns>
	protected virtual async ValueTask<IDictionary<string, string>?> InternalRefreshAsync(CancellationToken cancellationToken)
	{
		// Default implementation is to just return the existing tokens (ie success!)
		return await Tokens.GetAsync(cancellationToken);
	}
}


internal record ProviderFactory<TProvider, TSettings>(string Name, TProvider Provider, TSettings Settings, Func<TProvider, TSettings, TProvider> ConfigureProvider) : IProviderFactory
	where TProvider : IAuthenticationProvider
{
	private IAuthenticationProvider? configuredProvider;
	public IAuthenticationProvider AuthenticationProvider => configuredProvider ??= ConfigureProvider(Provider, Settings);
}

internal interface IProviderFactory
{
	IAuthenticationProvider AuthenticationProvider { get; }
	string Name { get; }
}
