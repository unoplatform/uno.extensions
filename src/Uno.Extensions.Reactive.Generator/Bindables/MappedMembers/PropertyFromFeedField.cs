using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator;

// Note: This also applies for State
internal record PropertyFromFeedField(IFieldSymbol _field, ITypeSymbol _valueType) : IMappedMember
{
	private readonly IFieldSymbol _field = _field;
	private readonly ITypeSymbol _valueType = _valueType;

	/// <inheritdoc />
	public string Name => _field.Name;

	/// <inheritdoc />
	public string GetBackingField()
		=> $"private {NS.Bindings}.Bindable<{_valueType.ToFullString()}> _{_field.Name};";

	/// <inheritdoc />
	public string GetDeclaration()
		=> new Property(_field.DeclaredAccessibility, _valueType, _field.GetPascalCaseName())
		{
			Getter = $"_{_field.Name}.GetValue()",
			Setter = $"_{_field.Name}.SetValue(value)",
		};

	/// <inheritdoc />
	public string GetInitialization()
		=> $"_{_field.Name} ??= new {NS.Bindings}.Bindable<{_valueType.ToFullString()}>(base.Property<{_valueType.ToFullString()}>(nameof({_field.Name}), {N.Ctor.Model}.{_field.Name} ?? throw new NullReferenceException(\"The feed field '{_field.Name}' is null. Public feeds fields must be initialized in the constructor.\")));";
}
