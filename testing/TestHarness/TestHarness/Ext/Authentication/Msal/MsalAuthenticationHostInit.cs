


namespace TestHarness.Ext.Authentication.MSAL;

public class MsalAuthenticationHostInit : BaseMsalHostInitialization
{
	protected override string[] ConfigurationFiles => new string[] { "TestHarness.Ext.Authentication.MSAL.appsettings.msalauthentication.json" };


	protected override IHostBuilder Custom(IHostBuilder builder, Window window)
	{
		return base.Custom(builder)
			.UseAuthentication(auth =>
					auth.AddMsal(window, msal =>
						msal
							.Scopes(new[] { "Tasks.Read", "User.Read", "Tasks.ReadWrite" })
							// No WithRedirectUri: the provider applies the platform default (localhost on
							// desktop, the WebAuthenticationBroker URI on WebAssembly, msal{ClientId}://auth
							// on Android). Builder(...) runs last, so a value set here would win everywhere.
							.Builder(msalBuilder =>
								msalBuilder
									.WithClientId("161a9fb5-3b16-487a-81a2-ac45dcc0ad3b"))
						// TODO: add ios support here - see https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3127
						//if (!string.IsNullOrWhiteSpace(settings.KeychainSecurityGroup))
						//{
						//	msalBuilder = msalBuilder.WithIosKeychainSecurityGroup(settings.KeychainSecurityGroup);
						//}
						)
				);
	}
}


