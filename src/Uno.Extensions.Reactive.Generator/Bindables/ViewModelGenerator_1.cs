using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator;

internal class ViewModelGenerator_1 : ICodeGenTool
{
	// Version 1 used with ViewModelGenTool Versions 1 and 2
	public string Version => "1";

	private readonly BindableGenerationContext _ctx;

	private List<INamedTypeSymbol> _toGenerate = new();

	public ViewModelGenerator_1(BindableGenerationContext ctx)
	{
		_ctx = ctx;
	}

	public string? GetBindableType(ITypeSymbol symbol)
	{
		if (symbol is INamedTypeSymbol { IsRecord: true } named && _ctx.GetDefaultCtor(named) is not null)
		{
			if (named is { NullableAnnotation: NullableAnnotation.Annotated })
			{
				named = named.OriginalDefinition;
			}

			_toGenerate.Add(named);
			return $"{symbol.ContainingNamespace}.Bindable{symbol.GetPascalCaseName()}";
		}

		return null;
	}

	public IEnumerable<(INamedTypeSymbol type, string code)> Generate()
	{
#pragma warning disable RS1024 // Compare symbols correctly => FALSE POSITIVE
		var generated = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
#pragma warning restore RS1024 // Compare symbols correctly
		while (Interlocked.Exchange(ref _toGenerate, new()) is { Count: > 0 } toGenerate)
		{
			foreach (var record in toGenerate)
			{
				if (!generated.Contains(record))
				{
					yield return (record, Generate(record));
					generated.Add(record);
				}
			}
		}
	}

	private string Generate(INamedTypeSymbol record)
	{
		var properties = record
			.GetProperties()
			.Where(prop => !prop.IsImplicitlyDeclared && prop.IsPublic() && !prop.IsStatic)
			.Select(prop =>
			{
				var camelName = prop.GetCamelCaseName();
				var canRead = prop.GetMethod is not null;
				var canWrite = prop.SetMethod is not null;

				Property property;
				string bindable, initializer;

				if (( // Either IImmutableList or the concrete ImmutableList
						prop.Type.Is(_ctx.IImmutableList, allowBaseTypes: false, out var immutableList)
						|| prop.Type.Is(_ctx.ImmutableList, allowBaseTypes: false, out immutableList)
					)
					&& GetBindableType(immutableList.TypeArguments.Single()) is {} itemBindableType)
				{
					var itemType = immutableList.TypeArguments.Single();
					var propertyInfo = prop.Type.Is(_ctx.ImmutableList, allowBaseTypes: false, out _)
						? GetPropertyInfo(_ctx.IImmutableList.Construct(itemType), (prop.Type, v => $"global::System.Collections.Immutable.ImmutableList.ToImmutableList({v})"))
						: GetPropertyInfo(prop.Type);

					bindable = $"{NS.Bindings}.BindableImmutableList<{itemType.ToFullString()}, {itemBindableType}>";
					initializer = $@"_{camelName} = new {bindable}(
						{propertyInfo.Align(6)},
						p => new {itemBindableType}(p));";
					property = new Property(prop.Type.DeclaredAccessibility, bindable, prop.Name)
					{
						Getter = $"_{camelName}"
					};
				}
				// Not supported for now since it would require us to build a generic Replace method which is not supported for all enumerable types
				//else if (prop.Type.IsOrImplements(_ctx.Enumerable, out var enumerable)
				//		&& GetBindableType(enumerable.TypeArguments.Single()) is {} itemBindable)
				//{
				//	bindable = $"{NS.Bindings}.BindableEnumerable<{prop.Type}, {enumerable.TypeArguments.Single()}, {itemBindable}>";
				//	initializer = $@"_{camelName} = new {bindable}({propertyInfo}, p => new {itemBindable}(p), #error TODO);";
				//	property = new Property(prop.Type.DeclaredAccessibility, bindable, prop.Name)
				//	{
				//		Getter = $"_{camelName}"
				//	};
				//}
				else if (GetBindableType(prop.Type) is {} subBindable)
				{
					bindable = subBindable;
					initializer = $@"_{camelName} = new {bindable}({GetPropertyInfo(prop.Type)});";
					property = new Property(prop.Type.DeclaredAccessibility, subBindable, prop.Name)
					{
						Getter = $"_{camelName}"
					};
				}
				else
				{
					bindable = $"{NS.Bindings}.Bindable<{prop.Type.ToFullString()}>";
					initializer = $@"_{camelName} = new {bindable}({GetPropertyInfo(prop.Type)});";
					property = Property.FromProperty(prop, allowInitOnlySetter: true) with
					{
						Getter = $"_{camelName}.GetValue()",
						Setter = $"_{camelName}.SetValue(value)",
						IsInit = false // Even if 'prop' is init only, we do allow set.
					};
				}

				return
				(
					symbol: prop,
					initializer: initializer,
					name: prop.Name,
					camelName: camelName,
					canRead: prop.GetMethod is not null,
					canWrite: prop.SetMethod is not null,
					bindable: bindable,
					property: property.ToString()
				);

				string GetPropertyInfo(ITypeSymbol type, (ITypeSymbol type, Func<string, string> cast)? concrete = null)
					=> $@"base.Property<{type.ToFullString()}>(
							nameof({prop.Name}),
							{record.GetCamelCaseName()} => {(canRead ? $"{record.GetCamelCaseName()}?.{prop.Name} ?? default({(concrete?.type ?? type).ToFullString()})" : $"default({(concrete?.type ?? type).ToFullString()})")},
							({record.GetCamelCaseName()}, {camelName}) => {(canWrite ? $"({record.GetCamelCaseName()} ?? CreateDefault()) with {{{prop.Name} = {concrete?.cast(camelName) ?? camelName}}}" : record.GetCamelCaseName())})";
			})
			.ToList();

		// Skip generating Value property if one is already defined in authored code
		var valueProperty = properties.Any(prop => prop.name == "Value")
			? null
			: new Property(record, "Value")
			{
				Getter = "base.GetValue()",
				Setter = "base.SetValue(value)"
			};

		var code = @$"{this.GetFileHeader()}

using System;
using System.Linq;
using System.Threading.Tasks;

namespace {record.ContainingNamespace}
{{
	{this.GetCodeGenAttribute()}
	[{NS.Bindings}.Bindable(typeof({record.ToFullString()}))]
	{record.GetAccessibilityAsCSharpCodeString()} sealed class Bindable{record.GetPascalCaseName()} : {NS.Bindings}.Bindable<{record}>
	{{
		{properties.Select(prop => $"private readonly {prop.bindable} _{prop.symbol.GetCamelCaseName()};").Align(2)}

		public Bindable{record.GetPascalCaseName()}({NS.Bindings}.BindablePropertyInfo<{record}> property)
			: base(property, hasValueProperty: {(valueProperty is null ? "false" : "true")})
		{{
			{properties.Select(prop => prop.initializer).Align(3)}
		}}

		private static {record} CreateDefault()
			=> new({_ctx.GetDefaultCtor(record)!.Parameters.Select(p => $"default({p.Type.ToFullString()})!").JoinBy(", ")});

		{valueProperty}

		{properties.Select(prop => prop.property).Align(2)}
	}}
}}
";

return code;
	}
}
