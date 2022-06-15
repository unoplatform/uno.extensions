namespace Uno.Extensions.Authentication.MSAL;

public static class HostBuilderExtensions
{
	//public static IHostBuilder UseAuthenticationFlow(
	//	this IHostBuilder builder,
	//	Action<IAuthenticationFlowBuilder>? configure = default)
	//{
	//	var authBuilder = builder.AsBuilder<AuthenticationFlowBuilder>();
	//	configure?.Invoke(authBuilder);

	//	return builder
	//		.ConfigureServices(services =>
	//		{
	//			services.AddSingleton<IAuthenticationFlow, AuthenticationFlow>();
	//			services.AddSingleton(authBuilder.Settings);
	//		});
	//}
}
