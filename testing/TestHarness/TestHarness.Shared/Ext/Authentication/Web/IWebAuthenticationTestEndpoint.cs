namespace TestHarness.Ext.Authentication.Web;


[Headers("Content-Type: application/json")]
public interface IWebAuthenticationTestEndpoint
{
	[Get("/api/test")]
	Task<WebAuthenticationTestItem[]> Test(CancellationToken ct);

}

public class WebAuthenticationTestItem
{
	[JsonPropertyName("type")]
	public string? Type { get; set; }

	[JsonPropertyName("value")]
	public string? Value { get; set; }
}

