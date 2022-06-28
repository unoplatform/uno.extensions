namespace Uno.Extensions.Authentication.MSAL;

internal record MsalAuthenticationSettings
{
	public PublicClientApplicationBuilder? Builder { get; private init; }

	private string? _clientId;
	public string? ClientId {
		get => _clientId;
		init
		{
			_clientId = value;
			Builder = PublicClientApplicationBuilder.Create(_clientId);
		}
	}

	public string[]? Scopes { get; init; }
}
