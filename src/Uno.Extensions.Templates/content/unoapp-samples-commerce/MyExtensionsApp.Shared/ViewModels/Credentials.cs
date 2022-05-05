//-:cnd:noEmit
namespace MyExtensionsApp.ViewModels;

public record Credentials
{
	public string? UserName { get; init; }

	public string? Password { get; init; }
}
