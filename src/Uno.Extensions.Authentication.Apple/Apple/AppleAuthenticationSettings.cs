namespace Uno.Extensions.Authentication.Apple;

internal record AppleAuthenticationSettings
{
	public bool FullNameScope { get; init; } = false;

	public bool EmailScope { get; init; } = false;
}
