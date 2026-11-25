using System.Text.Json.Serialization;

namespace Aspire.Hosting;

/// <summary>
/// The part of <c>uno.devserver disco --json</c> the AppHost reads: the solution the CLI serves and the dev servers running.
/// </summary>
/// <param name="SelectedSolutionPath">The solution the CLI selected in its working folder, null when none was found.</param>
/// <param name="ActiveServers">The dev servers running for this user.</param>
internal sealed record DevServerDiscovery(
	[property: JsonPropertyName("selectedSolutionPath")] string? SelectedSolutionPath,
	[property: JsonPropertyName("activeServers")] IReadOnlyList<DevServerActiveServer>? ActiveServers);
