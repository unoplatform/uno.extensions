namespace Uno.Extensions.Authentication;

public static class MsalAuthenticationBuilderExtensions
{
	public static IMsalAuthenticationBuilder Builder(
		this IMsalAuthenticationBuilder builder,
		Action<PublicClientApplicationBuilder> build
		)
	{
		if (builder is IBuilder<MsalAuthenticationSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				Build = build
			};
		}

		return builder;
	}

	public static IMsalAuthenticationBuilder Storage(
		this IMsalAuthenticationBuilder builder,
		Action<StorageCreationPropertiesBuilder> store
		)
	{
		if (builder is IBuilder<MsalAuthenticationSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				Store = store
			};
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
