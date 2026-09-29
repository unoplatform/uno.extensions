#if __WASM__
namespace Uno.Foundation;

/// <summary>
/// Minimal stand-in for <c>Uno.Foundation.WebAssemblyRuntime</c>, for the runtime-test engine only.
/// </summary>
/// <remarks>
/// Uno.UI.RuntimeTests.Engine ships as source and compiles into this assembly. Its embedded runner
/// guards the <c>Console.CancelKeyPress</c> registration - unsupported in the browser - with
/// <c>#if !__WASM__</c>, but Uno.Sdk no longer defines <c>__WASM__</c> for consumer projects, so the
/// registration is compiled in and throws <see cref="System.PlatformNotSupportedException"/> before a
/// single test runs (verified in build 228808). Defining the symbol fixes that and also switches on
/// three branches that read the test configuration from the URL query string via
/// <c>WebAssemblyRuntime.InvokeJS</c> - which does not exist here, because
/// <c>src/Directory.Build.props</c> deliberately drops the Uno.WinUI.Runtime.WebAssembly reference for
/// non-head projects (see the <c>__RemoveUnoRuntimeWasm</c> target). This supplies it; an internal type
/// is enough, since the engine sources are part of this assembly.
/// <para>
/// Delete this file and the <c>__WASM__</c> define in the csproj once the engine replaces those
/// <c>#if</c> blocks with a runtime check - the whole workaround exists because a compile-time symbol
/// is being used for a runtime concern.
/// </para>
/// </remarks>
internal static class WebAssemblyRuntime
{
	/// <summary>
	/// Evaluates a JavaScript snippet and returns its result as a string.
	/// </summary>
	/// <param name="javascript">The snippet to evaluate. The engine passes self-contained IIFEs.</param>
	/// <returns>The snippet's result, marshalled as a string.</returns>
	public static string InvokeJS(string javascript) => WebAssemblyRuntimeImports.Eval(javascript);
}

/// <summary>
/// <c>globalThis</c> bindings for <see cref="WebAssemblyRuntime"/>.
/// </summary>
/// <remarks>
/// Top-level rather than nested because the JS interop generator emits a partial declaration of every
/// containing type, which would force <see cref="WebAssemblyRuntime"/> to be partial for no other
/// reason. Same shape as <c>Uno.Extensions.Storage</c>'s <c>SessionStorageImports</c>.
/// </remarks>
internal static partial class WebAssemblyRuntimeImports
{
	// Bound to eval rather than a purpose-built helper because InvokeJS is by definition
	// "run this arbitrary snippet"; the engine's three call sites pass IIFEs, which eval's
	// global-scope semantics handle exactly like Uno's own implementation did.
	[System.Runtime.InteropServices.JavaScript.JSImport("globalThis.eval")]
	public static partial string Eval(string javascript);
}
#endif
