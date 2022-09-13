namespace TestHarness.Ext.Authentication.MSAL;

public class MsalAuthenticationMultiHostInit : BaseMsalHostInitialization
{
	protected override string[] ConfigurationFiles => new string[] { "TestHarness.Ext.Authentication.Custom.appsettings.dummyjson.json",
																	 "TestHarness.Ext.Authentication.Msal.appsettings.msalauthentication.json",
																	"TestHarness.Ext.Authentication.Msal.appsettings.multi.json"};
	protected override IHostBuilder Custom(IHostBuilder builder)
	{
		return base.Custom(builder)
			.ConfigureServices((context, services) =>
			{
				services
						.AddRefitClient<ICustomAuthenticationDummyJsonEndpoint>(context);
			})

				.UseAuthentication(auth =>
					auth
						.AddCustom(custom =>
							custom
								.Login(async (sp, dispatcher, credentials, cancellationToken) =>
								{
									if (credentials is null)
									{
										return default;
									}

									var authService = sp.GetRequiredService<ICustomAuthenticationDummyJsonEndpoint>();
									var name = credentials.FirstOrDefault(x => x.Key == nameof(CustomAuthenticationCredentials.Username)).Value;
									var password = credentials.FirstOrDefault(x => x.Key == nameof(CustomAuthenticationCredentials.Password)).Value;
									var creds = new CustomAuthenticationCredentials { Username = name, Password = password };
									var authResponse = await authService.Login(creds, CancellationToken.None);
									if (authResponse?.Token is not null)
									{
										credentials[TokenCacheExtensions.AccessTokenKey] = authResponse.Token;
										return credentials;
									}
									return default;
								})
								.Refresh(async (sp, tokenDictionary, cancellationToken) =>
								{
									var authService = sp.GetRequiredService<ICustomAuthenticationDummyJsonEndpoint>();
									var creds = new CustomAuthenticationCredentials
									{
										Username = tokenDictionary.TryGetValue(nameof(CustomAuthenticationCredentials.Username), out var name) ? name : string.Empty,
										Password = tokenDictionary.TryGetValue(nameof(CustomAuthenticationCredentials.Password), out var password) ? password : string.Empty
									};
									try
									{
										var authResponse = await authService.Login(creds, cancellationToken);
										if (authResponse?.Token is not null)
										{
											tokenDictionary[TokenCacheExtensions.AccessTokenKey] = authResponse.Token;
											return tokenDictionary;
										}
									}
									catch
									{
										// Ignore and just return null;
									}
									return default;
								}), name: "CustomCode")
						.AddCustom<ICustomAuthenticationDummyJsonEndpoint>(custom =>
						custom
							.Login(async (authService, dispatcher, credentials, cancellationToken) =>
							{
								if (credentials is null)
								{
									return default;
								}

								var name = credentials.FirstOrDefault(x => x.Key == nameof(CustomAuthenticationCredentials.Username)).Value;
								var password = credentials.FirstOrDefault(x => x.Key == nameof(CustomAuthenticationCredentials.Password)).Value;
								var creds = new CustomAuthenticationCredentials { Username = name, Password = password };
								var authResponse = await authService.Login(creds, cancellationToken);
								if (authResponse?.Token is not null)
								{
									credentials[TokenCacheExtensions.AccessTokenKey] = authResponse.Token;
									return credentials;
								}
								return default;
							})
							.Refresh(async (authService, tokenDictionary, cancellationToken) =>
							{
								var creds = new CustomAuthenticationCredentials
								{
									Username = tokenDictionary.TryGetValue(nameof(CustomAuthenticationCredentials.Username), out var name) ? name : string.Empty,
									Password = tokenDictionary.TryGetValue(nameof(CustomAuthenticationCredentials.Password), out var password) ? password : string.Empty
								};

								try
								{
									var authResponse = await authService.Login(creds, cancellationToken);
									if (authResponse?.Token is not null)
									{
										tokenDictionary[TokenCacheExtensions.AccessTokenKey] = authResponse.Token;
										return tokenDictionary;
									}
								}
								catch
								{
									// Ignore and return null
								}
								return default;
							}), name: "CustomService")
						.AddMsal(msal =>
							msal
								.Scopes(new[] { "Tasks.Read", "User.Read", "Tasks.ReadWrite" })
								.Builder(msalBuilder =>
									msalBuilder
										.WithClientId("161a9fb5-3b16-487a-81a2-ac45dcc0ad3b")
										.WithRedirectUri("uno-extensions://auth"))
							// TODO: add ios support here - see https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3127
							//if (!string.IsNullOrWhiteSpace(settings.KeychainSecurityGroup))
							//{
							//	msalBuilder = msalBuilder.WithIosKeychainSecurityGroup(settings.KeychainSecurityGroup);
							//}
							, name: "MsalCode")
						.AddMsal(name: "MsalConfig"));
	}

}


