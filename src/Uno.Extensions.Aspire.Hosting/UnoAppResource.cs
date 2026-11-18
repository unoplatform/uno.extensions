using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An Uno Platform app project. It starts no process: each head added to it is a resource of its own.
/// </summary>
[Experimental(UnoAppDiagnostics.ExperimentalId)]
public sealed class UnoAppResource : Resource
{
	internal UnoAppResource(string name, string projectPath, IReadOnlyList<string> targetFrameworks)
		: base(name)
	{
		ProjectPath = projectPath;
		ProjectDirectory = Path.GetDirectoryName(projectPath) ?? throw new ArgumentException("The project path has no folder.", nameof(projectPath));
		TargetFrameworks = targetFrameworks;
	}

	/// <summary>
	/// The full path of the app's project file.
	/// </summary>
	public string ProjectPath { get; }

	internal string ProjectDirectory { get; }

	/// <summary>
	/// The target frameworks the project file names, empty when it names none or computes them from properties.
	/// </summary>
	internal IReadOnlyList<string> TargetFrameworks { get; }

	/// <summary>
	/// Taken while a head of this app builds: heads share the project's obj folder.
	/// </summary>
	internal SemaphoreSlim BuildLock { get; } = new(1, 1);
}
