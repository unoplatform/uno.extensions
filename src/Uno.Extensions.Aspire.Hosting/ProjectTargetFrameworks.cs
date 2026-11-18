using System.Xml;
using System.Xml.Linq;

namespace Aspire.Hosting;

/// <summary>
/// Reads the target frameworks an app's project file names, without evaluating the project.
/// </summary>
internal static class ProjectTargetFrameworks
{
	/// <summary>
	/// The target frameworks in <c>TargetFramework</c> or <c>TargetFrameworks</c>; empty when the file names none,
	/// computes them from properties, or cannot be read.
	/// </summary>
	internal static IReadOnlyList<string> Read(string projectPath)
	{
		XDocument project;
		try
		{
			project = XDocument.Load(projectPath);
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException)
		{
			return [];
		}

		var values = project.Descendants()
			.Where(element => element.Name.LocalName is "TargetFramework" or "TargetFrameworks")
			.SelectMany(element => element.Value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			.ToList();

		// A property such as $(DotNetVersion) needs an evaluation this reader does not do.
		return values.Any(value => value.Contains("$(", StringComparison.Ordinal)) ? [] : values;
	}
}
