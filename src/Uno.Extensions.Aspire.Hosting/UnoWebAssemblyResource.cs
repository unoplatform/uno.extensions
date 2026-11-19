using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// The WebAssembly head of an Uno Platform app, served by the WebAssembly dev server. Its environment is written into the app at build time.
/// </summary>
[Experimental(UnoAppDiagnostics.ExperimentalId)]
public sealed class UnoWebAssemblyResource : UnoHeadResource
{
	/// <summary>
	/// The name of the endpoint the app is served on.
	/// </summary>
	public const string EndpointName = "http";

	internal UnoWebAssemblyResource(string name, UnoAppResource app)
		: base(name, app, "browserwasm")
	{
	}
}
