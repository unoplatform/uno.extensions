namespace Uno.Extensions.Authentication.Custom;

internal record CustomAuthenticationSettings
{
	public AsyncFunc<IServiceProvider, IDispatcher, ITokenCache, IDictionary<string, string>, bool>? LoginCallback { get; init; }
	public AsyncFunc<IServiceProvider, ITokenCache, bool>? RefreshCallback { get; init; }
	public AsyncFunc<IServiceProvider, IDispatcher, ITokenCache, bool>? LogoutCallback { get; init; }
}
