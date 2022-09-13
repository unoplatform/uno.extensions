
namespace Uno.Extensions.Authentication;

public abstract record BaseAuthenticationProvider(ILogger Logger, string Name, ITokenCache Tokens) : IAuthenticationProvider
{
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

	protected virtual ValueTask<IDictionary<string, string>?> InternalLoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		// Default behavior is to return null, which indicates unsuccessful login 
		return default;
	}

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

	protected virtual async ValueTask<bool> InternalLogoutAsync(IDispatcher? dispatcher, CancellationToken cancellationToken)
	{
		// Default implementation is to return true, which will cause the token cache to be flushed
		return true;
	}

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
