namespace Uno.Extensions.Authentication.Custom;

internal record CustomAuthenticationSettings
{
	public AsyncFunc<IServiceProvider, IDispatcher?, ITokenCache, IDictionary<string, string>, IDictionary<string, string>?>? LoginCallback { get; init; }
	public AsyncFunc<IServiceProvider, ITokenCache, IDictionary<string, string>, IDictionary<string, string>?>? RefreshCallback { get; init; }
	public AsyncFunc<IServiceProvider, IDispatcher?, ITokenCache, IDictionary<string, string>, bool>? LogoutCallback { get; init; }
}


internal record CustomAuthenticationSettings<TService>
{
	public AsyncFunc<TService, IServiceProvider, IDispatcher?, ITokenCache, IDictionary<string, string>, IDictionary<string, string>?>? LoginCallback { get; init; }
	public AsyncFunc<TService, IServiceProvider, ITokenCache, IDictionary<string, string>, IDictionary<string, string>?>? RefreshCallback { get; init; }
	public AsyncFunc<TService, IServiceProvider, IDispatcher?, ITokenCache, IDictionary<string, string>, bool>? LogoutCallback { get; init; }
}
