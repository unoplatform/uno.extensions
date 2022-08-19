


namespace TestHarness.Ext.Authentication.MSAL;

public class MsalAuthenticationHostInit : BaseMsalHostInitialization
{
	protected override string[] ConfigurationFiles => new string[] { "TestHarness.Ext.Authentication.Msal.appsettings.msalauthentication.json" };


	protected override IHostBuilder Custom(IHostBuilder builder)
	{
		return base.Custom(builder)
			.UseAuthentication(auth =>
					auth.AddMsal(msal =>
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
						)
				);

	}
}


