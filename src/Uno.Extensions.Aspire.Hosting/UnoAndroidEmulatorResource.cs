using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// The Android head of an Uno Platform app, deployed to a running emulator.
/// </summary>
[Experimental(UnoAppDiagnostics.ExperimentalId)]
public sealed class UnoAndroidEmulatorResource : UnoHeadResource
{
	internal UnoAndroidEmulatorResource(string name, UnoProjectResource project)
		: base(name, project, "android")
	{
	}
}
