

namespace Playground.Services.Endpoints;

[Headers("Content-Type: application/json")]
public interface IToDoTaskListEndpoint
{
	[Get("/todo/lists")]
	[Headers("Authorization: Bearer")]
	Task<ToDoTaskReponseData<ToDoTaskListData>> GetAllAsync(CancellationToken ct);

}

public class ToDoTaskReponseData<T>
{
	[JsonPropertyName("@odata.context")]
	public string? OdataContext { get; set; }

	[JsonPropertyName("value")]
	public List<T>? Value { get; set; }
}
public class ToDoTaskListData
{
	[JsonPropertyName("@odata.etag")]
	public string? Odata { get; set; }

	[JsonPropertyName("id")]
	public string? Id { get; set; }

	[JsonPropertyName("displayName")]
	public string? DisplayName { get; set; }

	[JsonPropertyName("isOwner")]
	public bool IsOwner { get; set; }

	[JsonPropertyName("isShared")]
	public bool IsShared { get; set; }

	[JsonPropertyName("wellknownListName")]
	public string? WellknownListName { get; set; }
}
