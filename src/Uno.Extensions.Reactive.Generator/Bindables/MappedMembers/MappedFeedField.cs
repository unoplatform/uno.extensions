using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator;

internal record MappedFeedField(IFieldSymbol _field, ITypeSymbol _valueType) : IMappedMember
{
	private readonly IFieldSymbol _field = _field;
	private readonly ITypeSymbol _valueType = _valueType;

	/// <inheritdoc />
	public string Name => _field.Name;

	/// <inheritdoc />
	public string? GetBackingField()
		=> null;

	/// <inheritdoc />
	public string GetDeclaration()
		=> $"{_field.DeclaredAccessibility.ToCSharpCodeString()} {NS.Reactive}.IFeed<{_valueType}> {_field.Name};";

	/// <inheritdoc />
	public string GetInitialization()
		=> $"{_field.Name} = new {NS.Bindings}.Bindable<{_valueType}>(base.Property<{_valueType}>(nameof({_field.Name}), {N.Ctor.Model}.{_field.Name} ?? throw new NullReferenceException(\"The feed field '{_field.Name}' is null. Public feeds fields must be initialized in the constructor.\")));";
}
