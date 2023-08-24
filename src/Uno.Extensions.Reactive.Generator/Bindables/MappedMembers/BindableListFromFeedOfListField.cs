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
	public string GetDeclaration()
		=> $"{Field.GetAccessibilityAsCSharpCodeString()} {NS.Reactive}.IListFeed<{ItemType.ToFullString()}> {Field.Name};"; // Note: This should be a State

	/// <inheritdoc />
	public string? GetInitialization()
		=> @$"
			if ({Field.Name} is null)
			{{
				var {Field.GetCamelCaseName()}Source = {N.Ctor.Model}.{Field.Name} ?? throw new NullReferenceException(""The list feed field '{Field.Name}' is null. Public feeds properties must be initialized in the constructor."");
				var {Field.GetCamelCaseName()}SourceListFeed = {N.ListFeed.Extensions.ToListFeed}<{CollectionType.ToFullString()}, {ItemType.ToFullString()}>({Field.GetCamelCaseName()}Source);
				var {Field.GetCamelCaseName()}SourceListState = {N.Ctor.Ctx}.GetOrCreateListState({Field.GetCamelCaseName()}SourceListFeed);
				{Field.Name} = {NS.Bindings}.BindableHelper.CreateBindableList(nameof({Field.Name}), {Field.GetCamelCaseName()}SourceListState);
			}}";
}
