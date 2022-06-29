namespace Uno.Extensions.Authentication;

public abstract record BaseAuthenticationService
(
	ITokenCache Tokens
) : IAuthenticationService
{
	public async ValueTask<bool> CanRefresh(CancellationToken? cancellationToken = default) => await Tokens.HasTokenAsync(cancellationToken) && await InternalCanRefresh(cancellationToken ?? CancellationToken.None);

	public async ValueTask<bool> LoginAsync(IDispatcher dispatcher, IDictionary<string, string>? credentials = default, CancellationToken? cancellationToken = default)
	{
		var tokens = await InternalLoginAsync(dispatcher, credentials, cancellationToken ?? CancellationToken.None);
		if (!await Tokens.SaveAsync(tokens, cancellationToken))
		{
			return false;
		}
		return await Tokens.HasTokenAsync(cancellationToken);
	}

	public async ValueTask<bool> LogoutAsync(IDispatcher dispatcher, CancellationToken? cancellationToken = default)
	{
		if (!await InternalLogoutAsync(dispatcher, cancellationToken ?? CancellationToken.None))
		{
			return false;
		}

		return await Tokens.ClearAsync(cancellationToken);
	}

	public async ValueTask<bool> RefreshAsync(CancellationToken? cancellationToken = default)
	{
		if (await CanRefresh())
		{
			var tokens = await InternalRefreshAsync(cancellationToken ?? CancellationToken.None);
			if(!await Tokens.SaveAsync(tokens, cancellationToken))
			{
				return false;
			}
		}

		return await Tokens.HasTokenAsync(cancellationToken);
	}

	protected async virtual ValueTask<bool> InternalCanRefresh(CancellationToken cancellationToken) => true;

	protected virtual ValueTask<IDictionary<string, string>?> InternalLoginAsync(IDispatcher dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken)
	{
		// Default behavior is to return null, which indicates unsuccessful login 
		return default;
	}

	protected virtual async ValueTask<bool> InternalLogoutAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
	{
		// Default implementation is to return true, which will cause the token cache to be flushed
		return true;
	}

	protected virtual async ValueTask<IDictionary<string, string>?> InternalRefreshAsync(CancellationToken cancellationToken)
	{
		// Default implementation is to just return the existing tokens (ie success!)
		return await Tokens.GetAsync();
	}
}
