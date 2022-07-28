namespace TestHarness.Ext.Authentication.Web;


[Headers("Content-Type: application/json")]
public interface IWebAuthenticationTestEndpoint
{
	[Get("/webauth/GetDataFacebook")]
	Task<IEnumerable<string>?> GetDataFacebook(CancellationToken ct);
}

