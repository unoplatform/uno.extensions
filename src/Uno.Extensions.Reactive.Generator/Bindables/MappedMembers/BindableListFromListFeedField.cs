using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator;

internal record BindableListFromListFeedField(IFieldSymbol _field, ITypeSymbol _valueType) : IMappedMember
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
		=> $"{_field.GetAccessibilityAsCSharpCodeString()} {NS.Reactive}.IListFeed<{_valueType.ToFullString()}> {_field.Name};"; // Note: This should be a State

	/// <inheritdoc />
	public string? GetInitialization()
		=> @$"
			if ({_field.Name} is null)
			{{
				var {_field.GetCamelCaseName()}Source = ({NS.Reactive}.IListFeed<{_valueType.ToFullString()}>) {N.Ctor.Model}.{_field.Name} ?? throw new NullReferenceException(""The list feed field '{_field.Name}' is null. Public feeds properties must be initialized in the constructor."");
				var {_field.GetCamelCaseName()}SourceListState = {N.Ctor.Ctx}.GetOrCreateListState({_field.GetCamelCaseName()}Source);
				{_field.Name} = {NS.Bindings}.BindableHelper.CreateBindableList(nameof({_field.Name}), {_field.GetCamelCaseName()}SourceListState);
			}}";
}
