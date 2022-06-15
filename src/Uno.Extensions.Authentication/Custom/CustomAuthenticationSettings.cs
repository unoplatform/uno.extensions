namespace Uno.Extensions.Authentication.Custom;

internal record CustomAuthenticationSettings
{
	public Func<IDispatcher, ITokenCache, IDictionary<string, string>, Task<bool>>? LoginCallback { get; init; }
	public Func<ITokenCache, Task<bool>>? RefreshCallback { get; init; }
	public Func<IDispatcher, ITokenCache, Task<bool>>? LogoutCallback { get; init; }
}
