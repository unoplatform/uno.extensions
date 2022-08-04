namespace Uno.Extensions.Authentication;

public abstract record BaseAuthenticationProvider(string Name, ITokenCache Tokens) : IAuthenticationProvider
{
	public async virtual ValueTask<bool> CanRefresh(CancellationToken cancellationToken) => true;

	public virtual ValueTask<IDictionary<string, string>?> LoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		// Default behavior is to return null, which indicates unsuccessful login 
		return default;
	}

	public virtual async ValueTask<bool> LogoutAsync(IDispatcher? dispatcher, CancellationToken cancellationToken)
	{
		// Default implementation is to return true, which will cause the token cache to be flushed
		return true;
	}

	public virtual async ValueTask<IDictionary<string, string>?> RefreshAsync(CancellationToken cancellationToken)
	{
		// Default implementation is to just return the existing tokens (ie success!)
		return await Tokens.GetAsync();
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
