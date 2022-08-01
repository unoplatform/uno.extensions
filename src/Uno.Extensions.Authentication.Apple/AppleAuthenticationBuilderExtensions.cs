namespace Uno.Extensions.Authentication;

public static class AppleAuthenticationBuilderExtensions
{

	public static IAppleAuthenticationBuilder IncludeFullName(
		this IAppleAuthenticationBuilder builder,
		bool includeFullName)
	{
		if (builder is IBuilder<AppleAuthenticationSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				FullNameScope = includeFullName
			};
		}

		return builder;
	}

	public static IAppleAuthenticationBuilder IncludeEmail(
	this IAppleAuthenticationBuilder builder,
	bool includeEmail)
	{
		if (builder is IBuilder<AppleAuthenticationSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				EmailScope = includeEmail
			};
		}

		return builder;
	}



}
