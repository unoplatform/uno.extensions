using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator;

internal record BindableListFeedField(IFieldSymbol _field, ITypeSymbol _valueType) : IMappedMember
{
	private readonly IFieldSymbol _field = _field;
	private readonly ITypeSymbol _valueType = _valueType;

	/// <inheritdoc />
	public string Name => _field.Name;

	/// <inheritdoc />
	public string GetDeclaration()
		=> $"{_field.GetAccessibilityAsCSharpCodeString()} {NS.Reactive}.IListFeed<{_valueType}> {_field.Name};"; // Note: This should be a State

	/// <inheritdoc />
	public string? GetInitialization()
		=> @$"{_field.Name} = new {NS.Bindings}.BindableListFeed<{_valueType}>(
				nameof({_field.Name}),
				{N.Ctor.Model}.{_field.Name} ?? throw new NullReferenceException(""The list feed field '{_field.Name}' is null. Public feeds properties must be initialized in the constructor.""),
				{N.Ctor.Ctx});";
}
