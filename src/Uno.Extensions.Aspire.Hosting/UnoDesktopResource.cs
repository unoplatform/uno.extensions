using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// The desktop head of an Uno Platform app.
/// </summary>
[Experimental(UnoAppDiagnostics.ExperimentalId)]
public sealed class UnoDesktopResource : UnoHeadResource
{
	internal UnoDesktopResource(string name, UnoAppResource app)
		: base(name, app, $"net{Environment.Version.Major}.0-desktop")
	{
	}
}
