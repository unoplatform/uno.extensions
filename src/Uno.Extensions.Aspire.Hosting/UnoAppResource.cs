using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An Uno Platform app project. It starts no process: each head added to it is a resource of its own.
/// </summary>
[Experimental(UnoAppDiagnostics.ExperimentalId)]
public sealed class UnoAppResource : Resource
{
	internal UnoAppResource(string name, string projectPath)
		: base(name)
	{
		ProjectPath = projectPath;
	}

	/// <summary>
	/// The full path of the app's project file.
	/// </summary>
	public string ProjectPath { get; }
}
