using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// The desktop head of an Uno Platform app.
/// </summary>
[Experimental(UnoAppDiagnostics.ExperimentalId)]
public sealed class UnoDesktopResource : UnoHeadResource
{
	internal UnoDesktopResource(string name, UnoProjectResource project)
		: base(name, project, "desktop")
	{
	}
}
