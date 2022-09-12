namespace TestHarness.Ext.Authentication.MSAL;

public class MsalAuthenticationSettingsHostInit : BaseMsalHostInitialization
{
	protected override string[] ConfigurationFiles => new string[] { "TestHarness.Ext.Authentication.Msal.appsettings.msalauthentication.json",
																	 "TestHarness.Ext.Authentication.Msal.appsettings.msal.json"};

	protected override IHostBuilder Custom(IHostBuilder builder)
	{
		return base.Custom(builder)
			.UseAuthentication(auth => auth.AddMsal());
	}
}


