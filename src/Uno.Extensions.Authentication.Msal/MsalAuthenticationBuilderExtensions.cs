namespace Uno.Extensions.Authentication.MSAL;

public static class MsalAuthenticationBuilderExtensions
{
	public static IMsalAuthenticationBuilder MsalClient(
		this IMsalAuthenticationBuilder builder,
		string clientId,
		Action<PublicClientApplicationBuilder> buildMsalClient
		)
	{
		if (builder is IBuilder<MsalAuthenticationSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				ClientId = clientId
			};
			if (authBuilder.Settings.Builder is not null)
			{
				buildMsalClient(authBuilder.Settings.Builder);
			}
		}

		return builder;
	}

	public static IMsalAuthenticationBuilder Scopes(
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
