using System;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Mocking;

/// <summary>
/// Entry point that activates MVUX mocking (spec 013). Inside the returned scope, every
/// <see cref="SourceContext"/> created (and its descendants) is mockable: its feeds are wrapped so their
/// source can be swapped at runtime. Outside any scope nothing is wrapped, so a live application pays
/// nothing (G9/R7).
/// </summary>
/// <remarks>
/// Granularity is the caller's: open it once at assembly-init to cover a whole test run, or around a
/// single <c>Create(...)</c>. The bit is captured on the context instance at construction, so a lazy first
/// subscription after the scope is disposed still wraps (D12).
/// </remarks>
public static class MockingService
{
	/// <summary>
	/// Opens a mocking-activation scope. Dispose it to stop marking future contexts as mockable;
	/// contexts already created inside the scope stay mockable for their own lifetime.
	/// </summary>
	public static IDisposable Enable()
		=> SourceContext.EnableMocking();
}
