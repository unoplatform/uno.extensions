﻿

namespace TestHarness.Ext.Authentication.Custom;


[Headers("Content-Type: application/json")]
public interface ICustomAuthenticationDummyJsonEndpoint
{
	[Post("/auth/login")]
	Task<CustomAuthenticationAuthResponse> Login(CustomAuthenticationCredentials credentials, CancellationToken ct);

	[Get("/products")]
	Task<CustomAuthenticationProductsResponse> Products(CancellationToken ct);

}

public class CustomAuthenticationCredentials
{
	[JsonPropertyName("username")]
	public string? Username { get; init; }
	[JsonPropertyName("password")]
	public string? Password { get; init; }
}

public class CustomAuthenticationAuthResponse
{
	[JsonPropertyName("token")]
	public string? Token { get; set; }
}

public class CustomAuthenticationProductsResponse:BaseCustomAuthenticationResponse
{
	[JsonPropertyName("products")]
	public CustomAuthenticationProduct[]? Products { get; set; }

}

public class BaseCustomAuthenticationResponse
{
	[JsonPropertyName("total")]
	public int Total { get; set; }
	[JsonPropertyName("skip")]
	public int Skip { get; set; }
	[JsonPropertyName("limit")]
	public int Limit { get; set; }
}

public class CustomAuthenticationProduct
{
	[JsonPropertyName("title")]
	public string? Title { get; set; }
}

//public class MsalAuthenticationToDoTaskReponseData<T>
//{
//	[JsonPropertyName("@odata.context")]
//	public string? OdataContext { get; set; }

//	[JsonPropertyName("value")]
//	public List<T>? Value { get; set; }
//}
//public class MsalAuthenticationToDoTaskListData
//{
//	[JsonPropertyName("@odata.etag")]
//	public string? Odata { get; set; }

//	[JsonPropertyName("id")]
//	public string? Id { get; set; }

//	[JsonPropertyName("displayName")]
//	public string? DisplayName { get; set; }

//	[JsonPropertyName("isOwner")]
//	public bool IsOwner { get; set; }

//	[JsonPropertyName("isShared")]
//	public bool IsShared { get; set; }

//	[JsonPropertyName("wellknownListName")]
//	public string? WellknownListName { get; set; }
//}
