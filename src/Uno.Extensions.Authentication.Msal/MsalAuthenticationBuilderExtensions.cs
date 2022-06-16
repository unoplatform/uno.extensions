namespace Uno.Extensions.Authentication.MSAL;

public static class MsalAuthenticationBuilderExtensions
{
	public static IMsalAuthenticationBuilder WithClientId(
		this IMsalAuthenticationBuilder builder,
		string clientId
		)
	{
		if (builder is IBuilder<MsalAuthenticationSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				ClientId = clientId
			};
			builder.MsalBuilder = authBuilder.Settings.Builder;
		}

		return builder;
	}

	public static IMsalAuthenticationBuilder WithScopes(
		this IMsalAuthenticationBuilder builder,
		string[] scopes
		)
	{
		if (builder is IBuilder<MsalAuthenticationSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				Scopes = scopes
			};
		}

		return builder;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static PublicClientApplicationBuilder WithWebRedirectUri(this PublicClientApplicationBuilder builder)
	{
		return builder.WithRedirectUri(WebAuthenticationBroker.GetCurrentApplicationCallbackUri().OriginalString);
	}
}
