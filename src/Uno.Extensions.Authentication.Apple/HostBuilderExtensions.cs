namespace Uno.Extensions.Authentication;

public static class HostBuilderExtensions
{
	public static IAuthenticationBuilder AddApple(
		this IAuthenticationBuilder builder,
		Action<IAppleAuthenticationBuilder>? configure = default,
		string name = AppleAuthenticationProvider.DefaultName)
	{
#if APPLEAUTH
		var authBuilder = builder.AsBuilder<AppleAuthenticationBuilder>();

		configure?.Invoke(authBuilder);

		return builder
			.AddAuthentication<AppleAuthenticationProvider, AppleAuthenticationSettings>(
				name,
				authBuilder.Settings,
				(provider, settings) => provider with { Name = name, Settings = settings });
#else
		return builder;
#endif
	}

	

}



