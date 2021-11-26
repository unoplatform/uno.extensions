using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator;

/// <summary>
/// A VM parameter that somehow implements IFeed&lt;TValue&gt; and which will be exposed on the BindableVM through a Bindable<T>.
/// </summary>
internal record FeedInput(IParameterSymbol Parameter, ITypeSymbol _valueType) : IInputInfo
{
	private readonly ITypeSymbol _valueType = _valueType;

	/// <inheritdoc />
	public IParameterSymbol Parameter { get; } = Parameter;

	public string GetBackingField()
		=> $"private readonly {GetBackingType()} _{Parameter.Name};";

	public (string? code, bool isOptional) GetCtorParameter()
		=> ($"{_valueType} default{Parameter.GetPascalCaseName()} = default", true);

	public string GetCtorInit(bool isInVmCtorParameters)
		=> isInVmCtorParameters
			? $"_{Parameter.Name} = new {GetBackingType()}(base.Property<{_valueType}>(nameof({Parameter.GetPascalCaseName()}), default{Parameter.GetPascalCaseName()}, out var {GetVMCtorParameter()}));"
			: $"_{Parameter.Name} = new {GetBackingType()}(base.Property<{_valueType}>(nameof({Parameter.GetPascalCaseName()}), default, out var {GetVMCtorParameter()}));";

	public string GetVMCtorParameter()
		=> $"{Parameter.Name}Subject";

	public string? GetPropertyInit()
		=> null;

	public string? GetProperty()
		=> $@"{_valueType.GetAccessibilityAsCSharpCodeString()} {_valueType} {Parameter.GetPascalCaseName()}
			{{
				get => _{Parameter.Name}.GetValue();
				set => _{Parameter.Name}.SetValue(value);
			}}";

	/// <inheritdoc />
	public virtual bool Equals(IInputInfo other)
		=> other is FeedInput otherState
			&& Parameter.Name.Equals(otherState.Parameter.Name, StringComparison.OrdinalIgnoreCase)
			&& SymbolEqualityComparer.Default.Equals(_valueType, otherState._valueType);
			// && _isEditable.Equals(otherState._isEditable); // Not needs to check for that, both are compatible

	private string GetBackingType()
		=> $"{NS.Reactive}.Bindable<{_valueType}>";
}
