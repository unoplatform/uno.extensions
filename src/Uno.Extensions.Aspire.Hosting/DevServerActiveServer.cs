using System.Text.Json.Serialization;

namespace Aspire.Hosting;

/// <summary>
/// A running dev server, as <c>uno.devserver disco --json</c> lists it. Every value is optional, so one incomplete entry
/// does not hide the others.
/// </summary>
/// <param name="ProcessId">The dev server's process.</param>
/// <param name="Port">The port it listens on.</param>
/// <param name="ParentProcessId">The process it stops with.</param>
/// <param name="SolutionPath">The solution it serves.</param>
/// <param name="StartTime">When its process started: a process with the same id and another start time is not it.</param>
internal sealed record DevServerActiveServer(
	[property: JsonPropertyName("processId")] int? ProcessId,
	[property: JsonPropertyName("port")] int? Port,
	[property: JsonPropertyName("parentProcessId")] int? ParentProcessId,
	[property: JsonPropertyName("solutionPath")] string? SolutionPath,
	[property: JsonPropertyName("startTime")] DateTimeOffset? StartTime = null);
