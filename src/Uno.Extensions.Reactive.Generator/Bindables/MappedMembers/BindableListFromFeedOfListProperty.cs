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
	// Note: This should be a State
	public string GetDeclaration() =>
		$$"""
		{{Property.GetAccessibilityAsCSharpCodeString()}} {{NS.Reactive}}.IListFeed<{{ItemType.ToFullString()}}> {{Property.Name}}
		{
			[global::System.Diagnostics.CodeAnalysis.DynamicDependency(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties, typeof({{ItemType.ToFullString()}}))]
			get;
			private set;
		}
		""";

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

	/// <inheritdoc />
	// The mock override is an IListFeed (not the model's IFeed<TCollection>) so the MockListFeed factories can be used directly.
	public string? GetMockPropertyType()
		=> $"{NS.Reactive}.IListFeed<{ItemType.ToFullString()}>";

	/// <inheritdoc />
	public string? GetMockInitialization(string mocks)
		=> $"{Property.Name} = {NS.Bindings}.BindableHelper.CreateBindableList(nameof({Property.Name}), {N.Ctor.Ctx}.GetOrCreateListState({mocks}.{Property.Name} ?? {NS.Mocks}.MockListFeed.Undefined<{ItemType.ToFullString()}>()));";
}
