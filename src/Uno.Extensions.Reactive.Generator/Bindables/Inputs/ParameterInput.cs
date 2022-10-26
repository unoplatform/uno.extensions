using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator;

/// <summary>
/// An unknown VM parameter that will be re-exposed in the BindableVM ctor
/// </summary>
internal record ParameterInput(IParameterSymbol Parameter) : IInputInfo
{
	/// <inheritdoc />
	public IParameterSymbol Parameter { get; } = Parameter;

	/// <inheritdoc />
	public string? GetBackingField() => null;

	/// <inheritdoc />
	public (string? code, bool isOptional) GetCtorParameter()
		=> Parameter.IsOptional
			? ($"{Parameter.Type} {Parameter.Name} = {Parameter.ExplicitDefaultValue ?? "default"}", true)
			: ($"{Parameter.Type} {Parameter.Name}", false);

	public string GetVMCtorParameter() => Parameter.Name;

	/// <inheritdoc />
	public string? GetCtorInit(bool isInVmCtorParameters) => null;

	public string? GetPropertyInit() => null;

	public Property? Property => null;

	/// <inheritdoc />
	public virtual bool Equals(IInputInfo other)
		=> other is ParameterInput otherCommon
			&& Parameter.Name.Equals(otherCommon.Parameter.Name, StringComparison.OrdinalIgnoreCase)
			&& SymbolEqualityComparer.Default.Equals(Parameter.Type, otherCommon.Parameter.Type);
}
