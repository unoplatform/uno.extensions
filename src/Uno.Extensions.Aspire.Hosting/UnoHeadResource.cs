using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// One head of an Uno Platform app, run with <c>dotnet run</c> for a single target framework.
/// </summary>
[Experimental(UnoAppDiagnostics.ExperimentalId)]
public abstract class UnoHeadResource : ExecutableResource
{
	/// <param name="name">The name of the head.</param>
	/// <param name="project">The project the head belongs to.</param>
	/// <param name="platform">The platform part of the target framework, for example <c>desktop</c>.</param>
	private protected UnoHeadResource(string name, UnoProjectResource project, string platform)
		: base(name, "dotnet", project.ProjectDirectory)
	{
		Project = project;
		TargetFramework = project.TargetFrameworks.FirstOrDefault(framework => IsForPlatform(framework, platform))
			?? $"net{Environment.Version.Major}.0-{platform}";
	}

	// The platform part may carry a version: net10.0-android36.0 is an Android target.
	private static bool IsForPlatform(string framework, string platform)
	{
		var separator = framework.IndexOf('-');
		return separator >= 0
			&& framework[(separator + 1)..].TrimEnd("0123456789.".ToCharArray()).Equals(platform, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// The project this head belongs to.
	/// </summary>
	public UnoProjectResource Project { get; }

	/// <summary>
	/// The target framework the head is built and run for: the one of its platform in the project file, else the
	/// AppHost's .NET version with the platform.
	/// </summary>
	public string TargetFramework { get; internal set; }

	/// <summary>
	/// The launch profile the head runs with, or <c>null</c> to run without one.
	/// </summary>
	public string? LaunchProfile { get; internal set; }

	/// <summary>
	/// Names of variables written into the app whatever their value, for a head whose environment is written into the app.
	/// </summary>
	internal HashSet<string> ClientEnvironmentNames { get; } = new(StringComparer.Ordinal);
}
