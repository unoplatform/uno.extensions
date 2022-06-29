namespace Uno.Extensions.Authentication.Custom;

internal record CustomAuthenticationSettings
{
	public AsyncFunc<IServiceProvider, IDispatcher, IDictionary<string, string>?, IDictionary<string, string>?>? LoginCallback { get; init; }
	public AsyncFunc<IServiceProvider, IDictionary<string, string>, IDictionary<string, string>?>? RefreshCallback { get; init; }
	public AsyncFunc<IServiceProvider, IDispatcher, IDictionary<string, string>, bool>? LogoutCallback { get; init; }
}


internal record CustomAuthenticationSettings<TService>
{
	public AsyncFunc<TService, IDispatcher, IDictionary<string, string>?, IDictionary<string, string>?>? LoginCallback { get; init; }
	public AsyncFunc<TService, IDictionary<string, string>, IDictionary<string, string>?>? RefreshCallback { get; init; }
	public AsyncFunc<TService, IDispatcher, IDictionary<string, string>, bool>? LogoutCallback { get; init; }
}
