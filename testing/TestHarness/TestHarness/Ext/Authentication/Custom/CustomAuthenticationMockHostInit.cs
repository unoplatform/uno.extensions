namespace TestHarness.Ext.Authentication.Custom;

public class CustomAuthenticationMockHostInit : BaseHostInitialization
{
	protected override IHostBuilder Custom(IHostBuilder builder)
	{
		return builder.UseAuthentication(auth =>
					auth.AddCustom(custom =>
							custom
								.Login(async (sp, dispatcher, credentials, cancellationToken) =>
								{
									if (credentials?.TryGetValue(nameof(CustomAuthenticationCredentials.Username), out var username) ?? false &&
										!username.IsNullOrEmpty())
									{
										credentials ??= new Dictionary<string, string>();
										credentials[TokenCacheExtensions.AccessTokenKey] = "SampleToken";
										credentials[TokenCacheExtensions.RefreshTokenKey] = "RefreshToken";
										credentials["Expiry"] = DateTime.Now.AddMinutes(5).ToString("g");
										return credentials;
									}

									return default;
								})
								.Refresh(async (sp, tokenDictionary, cancellationToken) =>
								{
									if ((tokenDictionary?.TryGetValue(TokenCacheExtensions.RefreshTokenKey, out var refreshToken) ?? false) &&
										!refreshToken.IsNullOrEmpty() &&
										(tokenDictionary?.TryGetValue("Expiry", out var expiry) ?? false) &&
										DateTime.TryParse(expiry, out var tokenExpiry) &&
										tokenExpiry > DateTime.Now)
									{
										tokenDictionary ??= new Dictionary<string, string>();
										tokenDictionary[TokenCacheExtensions.AccessTokenKey] = "NewSampleToken";
										tokenDictionary["Expiry"] = DateTime.Now.AddMinutes(5).ToString("g");
										return tokenDictionary;
									}

									return default;
								})
								, name: "CustomAuth")
				)
				.ConfigureServices(services =>
						services
								.AddSingleton<IAuthenticationRouteInfo>(
										_ => new AuthenticationRouteInfo<
												CustomAuthenticationLoginViewModel,
												CustomAuthenticationHomeViewModel>())
				);
	}


	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{

		views.Register(
				new ViewMap(ViewModel: typeof(AuthenticationShellViewModel)),
				new ViewMap<CustomAuthenticationLoginPage, CustomAuthenticationLoginViewModel>(),
				new ViewMap<CustomAuthenticationHomeTestBackendPage, CustomAuthenticationHomeTestBackendViewModel>()
				);


		routes
			.Register(
				new RouteMap("", View: views.FindByViewModel<AuthenticationShellViewModel>(),
						Nested: new RouteMap[]
						{
							new RouteMap("Login", View: views.FindByViewModel<CustomAuthenticationLoginViewModel>()),
							new RouteMap("Home", View: views.FindByViewModel<CustomAuthenticationHomeTestBackendViewModel>())
						}));
	}
}
