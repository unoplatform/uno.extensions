
namespace Uno.Extensions;

public static class HostBuilderExtensions
{
	public static IAuthenticationBuilder AddMsal(
		this IAuthenticationBuilder builder,
		Action<IMsalAuthenticationBuilder>? configure = default,
		string name = MsalAuthenticationProvider.DefaultName)
	{
		var hostBuilder = (builder as IBuilder)?.HostBuilder;
		if (hostBuilder is null)
		{
			return builder;
		}

		hostBuilder
			.UseConfiguration(configure: configBuilder =>
					configBuilder
						.Section<MsalConfiguration>(name)
				);


		var authBuilder = builder.AsBuilder<MsalAuthenticationBuilder>();
		configure?.Invoke(authBuilder);


		return builder
			.AddAuthentication<MsalAuthenticationProvider, MsalAuthenticationSettings>(
				name,
				authBuilder.Settings,
				(provider, settings) => {
					provider = provider with { Name = name, Settings = settings };
					provider.Build();
					return provider;
				});
	}

}
