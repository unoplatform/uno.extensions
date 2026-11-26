using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Reads the packages an app restored, from the <c>project.assets.json</c> NuGet writes in its obj folder.
/// </summary>
[Experimental(UnoAppDiagnostics.ExperimentalId)]
internal static class ProjectAssets
{
	/// <summary>
	/// Whether the restored app references <paramref name="packageId"/>, directly or not; <c>null</c> when there is no
	/// <c>obj/project.assets.json</c>, before the first restore or with another intermediate output path.
	/// </summary>
	internal static bool? References(UnoProjectResource app, string packageId) =>
		ReadLibraries(Path.Join(app.ProjectDirectory, "obj", "project.assets.json")) is { } libraries
			? FindVersion(libraries, packageId) is not null
			: null;

	/// <summary>
	/// The version of <paramref name="packageId"/> an assets file lists, directly or not; <c>null</c> when it lists none or
	/// does not exist.
	/// </summary>
	internal static string? Version(string assetsPath, string packageId) =>
		ReadLibraries(assetsPath) is { } libraries ? FindVersion(libraries, packageId) : null;

	private static IEnumerable<string>? ReadLibraries(string path)
	{
		if (!File.Exists(path))
		{
			return null;
		}

		using var stream = File.OpenRead(path);
		var assets = JsonSerializer.Deserialize(stream, ProjectAssetsJsonContext.Default.ProjectAssetsFile);
		return assets?.Libraries?.Keys ?? [];
	}

	// Library keys are "<id>/<version>".
	private static string? FindVersion(IEnumerable<string> libraries, string packageId)
	{
		var prefix = packageId + "/";
		return libraries.FirstOrDefault(library => library.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))?[prefix.Length..];
	}
}
