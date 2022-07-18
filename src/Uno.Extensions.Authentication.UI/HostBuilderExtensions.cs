using Uno.Extensions.Authentication.WinUI.Web.Social;

namespace Uno.Extensions.Authentication;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseAuthenticationFlow(
		this IHostBuilder builder,
		Action<IAuthenticationFlowBuilder>? configure = default)
	{
		var authBuilder = builder.AsBuilder<AuthenticationFlowBuilder>();
		configure?.Invoke(authBuilder);

		return builder
			.ConfigureServices(services =>
			{
				services.AddSingleton<IAuthenticationFlow, AuthenticationFlow>();
				services.AddSingleton(authBuilder.Settings);
			});
	}

	public static IAuthenticationBuilder AddWeb(
		this IAuthenticationBuilder builder,
		Action<IWebAuthenticationBuilder>? configure = default,
		string name = WebAuthenticationProvider.DefaultName)
	{
		var authBuilder = builder.AsBuilder<WebAuthenticationBuilder>();

		configure?.Invoke(authBuilder);

		return builder
			.AddAuthentication<WebAuthenticationProvider, WebAuthenticationSettings>(
				name,
				authBuilder.Settings,
				(provider, settings) => provider with { Name = name, Settings = settings });
	}

	//public static IAuthenticationBuilder AddFacebook(
	//	this IAuthenticationBuilder builder,
	//	Func<FacebookOptions, FacebookOptions> options,
	//	Action<IWebAuthenticationBuilder>? configure = default,
	//	string name = FacebookOptions.DefaultName)
	//{
	//	var fb = new FacebookOptions();
	//	fb = options(fb);

	//	var authBuilder = builder.AsBuilder<WebAuthenticationBuilder>();

	//	authBuilder.LoginStartUri(fb.StartUri);
	//	authBuilder.LoginCallbackUri(fb.CallbackUri);

	//	configure?.Invoke(authBuilder);

	//	return builder
	//		.AddAuthentication<WebAuthenticationProvider, WebAuthenticationSettings>(
	//			name,
	//			authBuilder.Settings,
	//			(provider, settings) => provider with { Name = name, Settings = settings });
	//}


	public static IAuthenticationBuilder AddCustom<TService>(
		this IAuthenticationBuilder builder,
		Action<IWebAuthenticationBuilder<TService>>? configure = default,
		string name = WebAuthenticationProvider.DefaultName)
			where TService : notnull

	{
		var authBuilder = builder.AsBuilder<WebAuthenticationBuilder<TService>>();

		configure?.Invoke(authBuilder);

		return builder
			.AddAuthentication<WebAuthenticationProvider, WebAuthenticationSettings<TService>>(
				name,
				authBuilder.Settings,
				(provider, settings) => provider with { Name = name, Settings = settings });
	}

}
