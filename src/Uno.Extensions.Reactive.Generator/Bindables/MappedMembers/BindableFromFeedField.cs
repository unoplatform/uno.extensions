using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator;

internal record BindableFromFeedField(IFieldSymbol _field, ITypeSymbol _valueType, string _bindableValueType) : IMappedMember
{
	private readonly IFieldSymbol _field = _field;
	private readonly ITypeSymbol _valueType = _valueType;
	private readonly string _bindableValueType = _bindableValueType;

	/// <inheritdoc />
	public string Name => _field.Name;

	/// <inheritdoc />
	public string? GetBackingField()
		=> null;

	/// <inheritdoc />
	public string GetDeclaration()
		=> $"{_field.GetAccessibilityAsCSharpCodeString()} {_bindableValueType} {_field.Name};";

	/// <inheritdoc />
	public string? GetInitialization()
		=> $"{_field.Name} ??= new {_bindableValueType}(base.Property<{_valueType.ToFullString()}>(nameof({_field.Name}), ({NS.Reactive}.IFeed<{_valueType.ToFullString()}>) {N.Ctor.Model}.{_field.Name} ?? throw new NullReferenceException(\"The feed field '{_field.Name}' is null. Public feeds fields must be initialized in the constructor.\")));";
}
