

namespace Uno.Extensions.Authentication.MSAL;

internal record MsalAuthenticationSettings
{
	public Action<PublicClientApplicationBuilder>? Build { get; init; }

	/// <summary>
	/// Applied to the <c>AcquireTokenInteractive</c> builder on every interactive sign-in.
	/// <see cref="Build"/> cannot reach it - the interactive modifiers (<c>WithPrompt</c>,
	/// <c>WithLoginHint</c>, <c>WithSystemWebViewOptions</c>, <c>WithCustomWebUi</c>) hang off a
	/// different builder type that only exists per-request.
	/// </summary>
	public Action<AcquireTokenInteractiveParameterBuilder>? InteractiveBuild { get; init; }

	public Action<StorageCreationPropertiesBuilder>? Store { get; init; }

	public string[]? Scopes { get; init; }
}
