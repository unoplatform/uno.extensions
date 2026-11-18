using System.Text.Json.Serialization;

namespace Aspire.Hosting;

/// <summary>
/// The part of <c>project.assets.json</c> that lists the restored libraries.
/// </summary>
/// <param name="Libraries">The restored libraries, keyed <c>&lt;id&gt;/&lt;version&gt;</c>.</param>
internal sealed record ProjectAssetsFile(
	[property: JsonPropertyName("libraries")] IReadOnlyDictionary<string, ProjectAssetsLibrary>? Libraries);
