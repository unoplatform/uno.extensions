using System.ComponentModel.DataAnnotations;

namespace Uno.Extensions.Authentication;

public interface IAuthenticationService
{
	string[] Providers { get; }
	ValueTask<bool> LoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials = default, string? provider = null, CancellationToken? cancellationToken = default);
	ValueTask<bool> RefreshAsync(CancellationToken? cancellationToken = default);
	ValueTask<bool> LogoutAsync(IDispatcher? dispatcher, CancellationToken? cancellationToken = default);
	ValueTask<bool> IsAuthenticated(CancellationToken? cancellationToken = default);
	event EventHandler LoggedOut;
}


public record LoginResponse(bool Success, ValidationResult[] Validations)
{
	public static implicit operator bool(LoginResponse response) => response.Success;
	public static implicit operator LoginResponse(bool success) => new LoginResponse(success);
}
