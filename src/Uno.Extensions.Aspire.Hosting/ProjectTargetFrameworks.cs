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
	/// computes one from a property, or cannot be read. A group that appends to the list, such as
	/// <c>$(TargetFrameworks);net10.0-ios</c>, adds its own values.
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
			.Where(value => !IsTheListItself(value))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

		// A property such as $(DotNetVersion) needs an evaluation this reader does not do, so the list is unknown.
		return values.Any(value => value.Contains("$(", StringComparison.Ordinal)) ? [] : values;
	}

	private static bool IsTheListItself(string value) =>
		value.Equals("$(TargetFrameworks)", StringComparison.OrdinalIgnoreCase)
		|| value.Equals("$(TargetFramework)", StringComparison.OrdinalIgnoreCase);
}
