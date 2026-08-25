using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Config;

/// <summary>
/// Metadata describing an eager constructor dependency of a model (spec 013 — MVUX mocking).
/// Emitted by the MVUX generator (ctor instrumentation) and also hand-declarable. Survives as
/// assembly metadata so the external mocking generator can constrain the generated
/// <c>Create(...)</c> factory (a service accessed eagerly in the ctor would NRE under null-inject,
/// so <c>Create</c> must require a real/fake value for that parameter).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class CtorDependencyAttribute : Attribute
{
	/// <summary>
	/// Creates a new constructor dependency descriptor for the given parameter.
	/// </summary>
	/// <param name="parameter">The name of the constructor parameter.</param>
	public CtorDependencyAttribute(string parameter)
	{
		Parameter = parameter;
	}

	/// <summary>
	/// The constructor parameter this descriptor applies to.
	/// </summary>
	public string Parameter { get; }

	/// <summary>
	/// <see langword="true"/> when the parameter is dereferenced eagerly during construction
	/// (constructor body, field/property initializer, or eager primary-ctor capture), so a
	/// null-injected value would throw. The generated <c>Create</c> must then require this parameter.
	/// </summary>
	public bool Eager { get; init; }

	/// <summary>
	/// The members whose eager access to the parameter triggered this descriptor (diagnostics/traceability).
	/// </summary>
	public string[] Members { get; init; } = Array.Empty<string>();
}
