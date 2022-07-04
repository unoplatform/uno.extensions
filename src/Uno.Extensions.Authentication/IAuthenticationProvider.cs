namespace Uno.Extensions.Authentication;

internal interface IAuthenticationProvider
{
	public string Name { get; }

	public ValueTask<bool> CanRefresh(CancellationToken cancellationToken);

	public ValueTask<IDictionary<string, string>?> LoginAsync(IDispatcher dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken);

	public ValueTask<bool> LogoutAsync(IDispatcher dispatcher, CancellationToken cancellationToken);

	public ValueTask<IDictionary<string, string>?> RefreshAsync(CancellationToken cancellationToken);
}
