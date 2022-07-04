namespace Uno.Extensions.Authentication;

internal static class AuthenticationBuilderExtensions
{
	public static TBuilder AsBuilder<TBuilder>(this IAuthenticationBuilder authBuilder) where TBuilder : IBuilder, new()
	{
		var hostBuilder = (authBuilder as IBuilder)!.HostBuilder!;
		if (hostBuilder is TBuilder builder)
		{
			return builder;
		}

		return new TBuilder { HostBuilder = hostBuilder };
	}

}
