namespace Uno.Extensions.Authentication.Custom;

internal record CustomAuthenticationSettings
{
	public AsyncFunc<IServiceProvider, IDispatcher, ITokenCache, IDictionary<string, string>, bool>? LoginCallback { get; init; }
	public AsyncFunc<IServiceProvider, ITokenCache, bool>? RefreshCallback { get; init; }
	public AsyncFunc<IServiceProvider, IDispatcher, ITokenCache, bool>? LogoutCallback { get; init; }
}


internal record CustomAuthenticationSettings<TService>
{
	public AsyncFunc<TService, IDispatcher, ITokenCache, IDictionary<string, string>, bool>? LoginCallback { get; init; }
	public AsyncFunc<TService, ITokenCache, bool>? RefreshCallback { get; init; }
	public AsyncFunc<TService, IDispatcher, ITokenCache, bool>? LogoutCallback { get; init; }
}
