using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator;

internal record BindableListFromFeedOfListField(IFieldSymbol Field, ITypeSymbol CollectionType, ITypeSymbol ItemType) : IMappedMember
{
	/// <inheritdoc />
	public string Name => Field.Name;

	/// <inheritdoc />
	public string? GetBackingField()
		=> null;

	/// <inheritdoc />
	// Note: This should be a State
	public string GetDeclaration() =>
		$$"""
		[global::System.Diagnostics.CodeAnalysis.DynamicDependency(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties, typeof({{ItemType.ToFullString()}}))]
		{{Field.GetAccessibilityAsCSharpCodeString()}} {{NS.Reactive}}.IListFeed<{{ItemType.ToFullString()}}> {{Field.Name}};
		""";

	/// <inheritdoc />
	public string? GetInitialization()
		=> @$"
			if ({Field.Name} is null)
			{{
				var {Field.GetCamelCaseName()}Source = ({NS.Reactive}.IFeed<{CollectionType.ToFullString()}>) {N.Ctor.Model}.{Field.Name} ?? throw new NullReferenceException(""The list feed field '{Field.Name}' is null. Public feeds properties must be initialized in the constructor."");
				var {Field.GetCamelCaseName()}SourceListFeed = {N.ListFeed.Extensions.ToListFeed}<{CollectionType.ToFullString()}, {ItemType.ToFullString()}>({Field.GetCamelCaseName()}Source);
				var {Field.GetCamelCaseName()}SourceListState = {N.Ctor.Ctx}.GetOrCreateListState({Field.GetCamelCaseName()}SourceListFeed);
				{Field.Name} = {NS.Bindings}.BindableHelper.CreateBindableList(nameof({Field.Name}), {Field.GetCamelCaseName()}SourceListState);
			}}";

	/// <inheritdoc />
	// The mock override is an IListFeed (not the model's IFeed<TCollection>) so the MockListFeed factories can be used directly.
	public string? GetMockPropertyType()
		=> $"{NS.Reactive}.IListFeed<{ItemType.ToFullString()}>";

	/// <inheritdoc />
	public string? GetMockInitialization(string mocks)
		=> $"{Field.Name} = {NS.Bindings}.BindableHelper.CreateBindableList(nameof({Field.Name}), {N.Ctor.Ctx}.GetOrCreateListState({mocks}.{Field.Name} ?? {NS.Mocks}.MockListFeed.Undefined<{ItemType.ToFullString()}>()));";
}
