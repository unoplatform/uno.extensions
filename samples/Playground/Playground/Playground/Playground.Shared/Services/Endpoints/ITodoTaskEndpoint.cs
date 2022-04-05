

namespace Playground.Services.Endpoints;

[Headers("Content-Type: application/json")]
public interface ITodoTaskEndpoint
{
	[Get("/todo/lists/{listId}/tasks/{taskId}")]
	[Headers("Authorization: Bearer")]
	Task<TodoTask> GetTask(string listId, string taskId, CancellationToken ct);

	[Post("/todo/lists/{listId}/tasks")]
	[Headers("Authorization: Bearer")]
	Task<TodoTask> CreateTask(string listId, [Body] object newTask, CancellationToken ct);

	[Patch("/todo/lists/{listId}/tasks/{taskId}")]
	[Headers("Authorization: Bearer")]
	Task<TodoTask> UpdateTask(string listId, string taskId, [Body] object updatedTask, CancellationToken ct);

	[Delete("/todo/lists/{listId}/tasks/{taskId}")]
	[Headers("Authorization: Bearer")]
	Task DeleteTask(string listId, string taskId, CancellationToken ct);
}

public class TodoTask
{
	[JsonPropertyName("id")]
	public string? Id { get; set; }

	[JsonPropertyName("status")]
	public string? Status { get; set; }

	[JsonPropertyName("title")]
	public string? Title { get; set; }
}
