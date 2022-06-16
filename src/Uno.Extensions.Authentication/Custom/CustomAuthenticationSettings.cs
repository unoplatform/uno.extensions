namespace Uno.Extensions.Authentication.Custom;

internal record CustomAuthenticationSettings
{
	public AsyncFunc<IDispatcher, ITokenCache, IDictionary<string, string>, bool>? LoginCallback { get; init; }
	public AsyncFunc<ITokenCache, bool>? RefreshCallback { get; init; }
	public AsyncFunc<IDispatcher, ITokenCache, bool>? LogoutCallback { get; init; }
}
