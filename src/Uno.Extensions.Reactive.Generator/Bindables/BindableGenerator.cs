using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator;

internal class BindableGenerator
{
	private readonly BindableGenerationContext _ctx;

	private List<INamedTypeSymbol> _toGenerate = new();

	public BindableGenerator(BindableGenerationContext ctx)
	{
		_ctx = ctx;
	}

	public string? GetBindableType(ITypeSymbol symbol)
	{
		if (symbol is INamedTypeSymbol { IsRecord: true} named && GetDefaultCtor(named) is not null)
		{
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
			.Where(prop => !prop.IsImplicitlyDeclared)
			.Select(prop =>
			{
				var subBindable = GetBindableType(prop.Type);
				var camelName = prop.GetCamelCaseName();
				var property = subBindable is not null
					? new Property(prop.Type.DeclaredAccessibility, subBindable, prop.Name)
					{
						Getter = $"_{camelName}"
					}
					: Property.FromProperty(prop) with
					{
						Getter = $"_{camelName}.GetValue()",
						Setter = $"_{camelName}.SetValue(value)",
						IsInit = false // Even if 'prop' is init only, we do allow set.
					};

				return
				(
					symbol: prop,
					name: prop.Name,
					camelName: camelName,
					canRead: prop.GetMethod is not null,
					canWrite: prop.SetMethod is not null,
					hasBindable: subBindable is not null,
					bindable: subBindable ?? $"{NS.Bindings}.Bindable<{prop.Type}>",
					property: property.ToString()
				);
			})
			.ToList();
		var valueProperty = properties.Any(prop => prop.name == "Value")
			? null
			: new Property(record, "Value")
			{
				Getter = "base.GetValue()",
				Setter = "base.SetValue(value)"
			};

		var code = @$"#nullable enable
#pragma warning disable

using System;
using System.Linq;
using System.Threading.Tasks;

namespace {record.ContainingNamespace}
{{
	{record.GetAccessibilityAsCSharpCodeString()} sealed class Bindable{record.GetPascalCaseName()} : {NS.Bindings}.Bindable<{record}>
	{{
		{properties.Select(prop => $"private readonly {prop.bindable} _{prop.symbol.GetCamelCaseName()};").Align(2)}

		public Bindable{record.GetPascalCaseName()}({NS.Bindings}.BindablePropertyInfo<{record}> property)
			: base(property, hasValueProperty: {(valueProperty is null ? "false" : "true")})
		{{
			{properties
				.Select(prop => $@"
					_{prop.camelName} = new {prop.bindable}(base.Property<{prop.symbol.Type}>(
						nameof({prop.name}),
						{record.GetCamelCaseName()} => {(prop.canRead ? $"{record.GetCamelCaseName()}?.{prop.name} ?? default" : $"default({prop.symbol.Type})")},
						({record.GetCamelCaseName()}, {prop.camelName}) => {(prop.canWrite ? $"({record.GetCamelCaseName()} ?? CreateDefault()) with {{{prop.name} = {prop.camelName}}}" : record.GetCamelCaseName())}));")
				.Align(3)}
		}}

		private static {record} CreateDefault()
			=> new({GetDefaultCtor(record)!.Parameters.Select(p => $"default({p.Type})!").JoinBy(", ")});

		{valueProperty}

		{properties.Select(prop => prop.property).Align(2)}
	}}
}}
";

return code;
	}

	private IMethodSymbol? GetDefaultCtor(INamedTypeSymbol record)
	{
		return record
			.Constructors
			.Where(ctor => ctor.IsAccessible() && !IsCloneCtor(ctor))
			.OrderBy(ctor => ctor.HasAttributes(_ctx.DefaultRecordCtor) ? 0 : 1)
			.ThenBy(ctor => ctor.Parameters.Length)
			.FirstOrDefault();

		bool IsCloneCtor(IMethodSymbol ctor)
			=> ctor.Parameters is {Length: 1} parameters && SymbolEqualityComparer.Default.Equals(parameters[0].Type, record);
	}
}
