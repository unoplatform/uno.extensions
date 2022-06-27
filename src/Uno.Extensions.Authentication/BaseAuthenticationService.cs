namespace Uno.Extensions.Authentication;

public abstract record BaseAuthenticationService
(
	ITokenCache Tokens
) : IAuthenticationService
{
	public virtual Task<bool> CanRefresh() => Tokens.HasTokenAsync();

	public Task<bool> LoginAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
	{
		return LoginAsync(dispatcher, default, cancellationToken);
	}

	public abstract Task<bool> LoginAsync(IDispatcher dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken);

	public async Task<bool> LogoutAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
	{
		if (!await InternalLogoutAsync(dispatcher, cancellationToken))
		{
			return false;
		}

		await Tokens.ClearAsync();
		return true;
	}

	protected virtual async Task<bool> InternalLogoutAsync(IDispatcher dispatcher, CancellationToken cancellationToken)
	{
		return true;
	}

	public async Task<bool> RefreshAsync(CancellationToken cancellationToken)
	{
		if (await CanRefresh())
		{
			return await InternalRefreshAsync(cancellationToken);
		}

		return await Tokens.HasTokenAsync();
	}

	protected virtual async Task<bool> InternalRefreshAsync(CancellationToken cancellationToken)
	{
		return await Tokens.HasTokenAsync();
	}
}
