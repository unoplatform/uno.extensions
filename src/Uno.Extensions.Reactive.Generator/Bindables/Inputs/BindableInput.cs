using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator;

/// <summary>
/// A VM parameter that somehow implements IFeed&lt;TValue&gt; and which will be exposed on the BindableVM
/// as de-normalized properties through a generated BindableTValue class (cf. <see cref="ViewModelGenerator_2"/>).
/// </summary>
internal record BindableInput(IParameterSymbol Parameter, ITypeSymbol _valueType, string _bindableType) : IInputInfo
{
	private readonly ITypeSymbol _valueType = _valueType;

	/// <inheritdoc />
	public IParameterSymbol Parameter { get; } = Parameter;

	public string? GetBackingField()
		=> null;

	public (string? code, bool isOptional) GetCtorParameter()
		=> ($"{_valueType} default{Parameter.GetPascalCaseName()} = default", true);

	public string GetCtorInit(bool isInVmCtorParameters)
		=> isInVmCtorParameters
			? $"{Parameter.GetPascalCaseName()} = new {_bindableType}(base.Property<{_valueType}>(nameof({Parameter.GetPascalCaseName()}), default{Parameter.GetPascalCaseName()}, out var {GetVMCtorParameter()}));"
			: $"{Parameter.GetPascalCaseName()} = new {_bindableType}(base.Property<{_valueType}>(nameof({Parameter.GetPascalCaseName()}), default, out var {GetVMCtorParameter()}));";

	public string GetVMCtorParameter()
		=> $"{Parameter.Name}Subject";

	public string? GetPropertyInit()
		=> null;

	public Property Property => new(_valueType.DeclaredAccessibility, _bindableType, Parameter.GetPascalCaseName()) { HasGetter = true };

	/// <inheritdoc />
	public virtual bool Equals(IInputInfo other)
		=> other is BindableInput otherBindable
			&& Parameter.Name.Equals(otherBindable.Parameter.Name, StringComparison.OrdinalIgnoreCase)
			&& SymbolEqualityComparer.Default.Equals(_valueType, otherBindable._valueType);
}
