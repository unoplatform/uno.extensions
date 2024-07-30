namespace TestHarness.Ext.Authentication.Oidc;


[Headers("Content-Type: application/json")]
public interface IOidcAuthenticationTestEndpoint
{
	[Get("/api/test")]
	Task<OidcAuthenticationTestItem[]> Test(CancellationToken ct);

}

public class OidcAuthenticationTestItem
{
	[JsonPropertyName("type")]
	public string? Type { get; set; }

	[JsonPropertyName("value")]
	public string? Value { get; set; }
}

