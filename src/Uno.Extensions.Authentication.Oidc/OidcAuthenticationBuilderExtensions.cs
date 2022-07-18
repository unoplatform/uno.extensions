namespace Uno.Extensions.Authentication;

public static class OidcAuthenticationBuilderExtensions
{
	public static IOidcAuthenticationBuilder Authority(
		this IOidcAuthenticationBuilder builder,
		string authority)
		=> builder.ChangeOption(options => options.Authority = authority);

	public static IOidcAuthenticationBuilder ClientId(
		this IOidcAuthenticationBuilder builder,
		string clientId)
		=> builder.ChangeOption(options => options.ClientId = clientId);

	public static IOidcAuthenticationBuilder ClientSecret(
		this IOidcAuthenticationBuilder builder,
		string clientSecret)
		=> builder.ChangeOption(options => options.ClientSecret = clientSecret);

	public static IOidcAuthenticationBuilder Scope(
	this IOidcAuthenticationBuilder builder,
	string scope)
	=> builder.ChangeOption(options => options.Scope = scope);

	public static IOidcAuthenticationBuilder RedirectUri(
	this IOidcAuthenticationBuilder builder,
	string redirectUri)
	=> builder.ChangeOption(options => options.RedirectUri = redirectUri);

	public static IOidcAuthenticationBuilder PostLogoutRedirectUri(
	this IOidcAuthenticationBuilder builder,
	string postLogoutRedirectUri)
	=> builder.ChangeOption(options => options.PostLogoutRedirectUri = postLogoutRedirectUri);

	private static IOidcAuthenticationBuilder ChangeOption(
		this IOidcAuthenticationBuilder builder,
		Action<OidcClientOptions> change)
	{
		if (builder is IBuilder<OidcAuthenticationSettings> authBuilder)
		{
			if (authBuilder.Settings.Options is null)
			{
				authBuilder.Settings = authBuilder.Settings with
				{
					Options = new OidcClientOptions()
				};

			}
			change(authBuilder.Settings.Options);
		}

		return builder;
	}

}
