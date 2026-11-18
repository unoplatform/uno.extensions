using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// One head of an Uno Platform app, run with <c>dotnet run</c> for a single target framework.
/// </summary>
[Experimental(UnoAppDiagnostics.ExperimentalId)]
public abstract class UnoHeadResource : ExecutableResource
{
	private protected UnoHeadResource(string name, UnoAppResource app, string targetFramework)
		: base(name, "dotnet", Path.GetDirectoryName(app.ProjectPath)!)
	{
		App = app;
		TargetFramework = targetFramework;
	}

	/// <summary>
	/// The app this head belongs to.
	/// </summary>
	public UnoAppResource App { get; }

	/// <summary>
	/// The target framework the head is built and run for.
	/// </summary>
	public string TargetFramework { get; internal set; }

	/// <summary>
	/// The launch profile the head runs with, or <c>null</c> to run without one.
	/// </summary>
	public string? LaunchProfile { get; internal set; }
}
