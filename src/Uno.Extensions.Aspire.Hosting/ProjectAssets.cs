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
	internal static bool? References(UnoProjectResource app, string packageId)
	{
		var path = Path.Join(app.ProjectDirectory, "obj", "project.assets.json");
		if (!File.Exists(path))
		{
			return null;
		}

		using var stream = File.OpenRead(path);
		var assets = JsonSerializer.Deserialize(stream, ProjectAssetsJsonContext.Default.ProjectAssetsFile);

		// Library keys are "<id>/<version>".
		var prefix = packageId + "/";
		return assets?.Libraries?.Keys.Any(library => library.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) ?? false;
	}
}
