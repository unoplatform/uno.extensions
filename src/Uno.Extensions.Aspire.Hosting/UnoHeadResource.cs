using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// One head of an Uno Platform app, run with <c>dotnet run</c> for a single target framework.
/// </summary>
[Experimental(UnoAppDiagnostics.ExperimentalId)]
public abstract class UnoHeadResource : ExecutableResource
{
	/// <param name="name">The name of the head.</param>
	/// <param name="app">The app the head belongs to.</param>
	/// <param name="platform">The platform part of the target framework, for example <c>desktop</c>.</param>
	private protected UnoHeadResource(string name, UnoAppResource app, string platform)
		: base(name, "dotnet", app.ProjectDirectory)
	{
		App = app;
		TargetFramework = app.TargetFrameworks.FirstOrDefault(framework => framework.EndsWith($"-{platform}", StringComparison.OrdinalIgnoreCase))
			?? $"net{Environment.Version.Major}.0-{platform}";
	}

	/// <summary>
	/// The app this head belongs to.
	/// </summary>
	public UnoAppResource App { get; }

	/// <summary>
	/// The target framework the head is built and run for: the one of its platform in the project file, else the
	/// AppHost's .NET version with the platform.
	/// </summary>
	public string TargetFramework { get; internal set; }

	/// <summary>
	/// The launch profile the head runs with, or <c>null</c> to run without one.
	/// </summary>
	public string? LaunchProfile { get; internal set; }
}
