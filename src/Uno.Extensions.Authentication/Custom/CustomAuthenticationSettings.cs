namespace Uno.Extensions.Authentication.Custom;

internal record CustomAuthenticationSettings
{
	public AsyncFunc<IServiceProvider, IDispatcher, IReadonlyTokenCache, IDictionary<string, string>?, IDictionary<string, string>?>? LoginCallback { get; init; }
	public AsyncFunc<IServiceProvider, IReadonlyTokenCache, IDictionary<string, string>?>? RefreshCallback { get; init; }
	public AsyncFunc<IServiceProvider, IDispatcher, IReadonlyTokenCache, bool>? LogoutCallback { get; init; }
}


internal record CustomAuthenticationSettings<TService>
{
	public AsyncFunc<TService, IDispatcher, IReadonlyTokenCache, IDictionary<string, string>?, IDictionary<string, string>?>? LoginCallback { get; init; }
	public AsyncFunc<TService, IReadonlyTokenCache, IDictionary<string, string>?>? RefreshCallback { get; init; }
	public AsyncFunc<TService, IDispatcher, IReadonlyTokenCache, bool>? LogoutCallback { get; init; }
}
