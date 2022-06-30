namespace TestHarness.Ext.Authentication.MSAL;

public class MsalAuthenticationSettingsHostInit : IHostInitialization
{
	public IHost InitializeHost()
	{

		return UnoHost
				.CreateDefaultBuilder()
				.Defaults(this)

				// Only use this syntax for UI tests - use UseConfiguration in apps
				.ConfigureAppConfiguration((ctx, b) =>
				{
					b.AddEmbeddedConfigurationFile<App>("TestHarness.Ext.Authentication.Msal.appsettings.msalauthentication.json");
					b.AddEmbeddedConfigurationFile<App>("TestHarness.Ext.Authentication.Msal.appsettings.msal.json");
				})

				.UseAuthentication(auth => auth.AddMsal())

				.Build(enableUnoLogging: true);
	}

}


