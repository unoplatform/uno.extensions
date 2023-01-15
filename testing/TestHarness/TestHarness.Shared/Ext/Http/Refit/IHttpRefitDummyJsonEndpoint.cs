namespace TestHarness.Ext.Authentication.Custom;

[Headers("Content-Type: application/json")]
public interface IHttpRefitDummyJsonEndpoint
{
	[Get("/products")]
	Task<HttpRefitProductsResponse> Products(CancellationToken ct);
}

public class HttpRefitProductsResponse : BaseHttpRefitResponse
{
	[JsonPropertyName("products")]
	public HttpRefitProduct[]? Products { get; set; }

}

public class BaseHttpRefitResponse
{
	[JsonPropertyName("total")]
	public int Total { get; set; }
	[JsonPropertyName("skip")]
	public int Skip { get; set; }
	[JsonPropertyName("limit")]
	public int Limit { get; set; }
}

public class HttpRefitProduct
{
	[JsonPropertyName("title")]
	public string? Title { get; set; }
}
