namespace Playground.Models;

public record  AppInfo
{
	public string? Title { get; init; }
	public string? Platform { get; init; }
	public bool Mock { get; init; }

}
