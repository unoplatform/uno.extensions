using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator;

internal record BindableListFromFeedOfListProperty(IPropertySymbol Property, ITypeSymbol CollectionType, ITypeSymbol ItemType) : IMappedMember
{
	/// <inheritdoc />
	public string Name => Property.Name;

	/// <inheritdoc />
	public string? GetBackingField()
		=> null;

	/// <inheritdoc />
	public string GetDeclaration()
		=> $"{Property.GetAccessibilityAsCSharpCodeString()} {NS.Reactive}.IListFeed<{ItemType.ToFullString()}> {Property.Name} {{ get; private set; }}"; // Note: This should be a State

	/// <inheritdoc />
	public string GetInitialization()
		=> @$"
			if ({Property.Name} is null)
			{{
				var {Property.GetCamelCaseName()}Source = ({NS.Reactive}.IFeed<{CollectionType.ToFullString()}>) {N.Ctor.Model}.{Property.Name} ?? throw new NullReferenceException(""The list feed property '{Property.Name}' is null. Public feeds properties must be initialized in the constructor."");
				var {Property.GetCamelCaseName()}SourceListFeed = {N.ListFeed.Extensions.ToListFeed}<{CollectionType.ToFullString()}, {ItemType.ToFullString()}>({Property.GetCamelCaseName()}Source);
				var {Property.GetCamelCaseName()}SourceListState = {N.Ctor.Ctx}.GetOrCreateListState({Property.GetCamelCaseName()}SourceListFeed);
				{Property.Name} = {NS.Bindings}.BindableHelper.CreateBindableList(nameof({Property.Name}), {Property.GetCamelCaseName()}SourceListState);
			}}";
}
