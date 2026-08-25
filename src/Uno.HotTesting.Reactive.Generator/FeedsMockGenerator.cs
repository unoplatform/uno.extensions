using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Uno.HotTesting.Reactive.Generator;

/// <summary>
/// Consumer-side generator (spec 013, tiers 2/3). Runs in a test/preview project, reads the app
/// metadata (models carrying <c>FeedDependency</c>/<c>CtorDependency</c> attributes + their generated
/// view-models) and emits, per model:
///   - <c>record {Model}Mock</c> — required service-dependent inputs, optional derived + command overrides;
///   - <c>{Vm}.Create(...)</c> — null-inject construction (under the ambient MockingService scope);
///   - <c>SetModel(this {Vm}, {Model}Mock)</c> — strongly-typed swaps via <c>MockingService</c>.
/// Strongly typed end to end (D9); reuses the <c>Uno.HotTesting.Reactive</c> vocabulary (FeedMock /
/// ListFeedMock / CommandMock).
/// </summary>
[Generator]
public sealed class FeedsMockGenerator : ISourceGenerator
{
	private const string FeedDependencyAttribute = "Uno.Extensions.Reactive.Config.FeedDependencyAttribute";
	private const string ModelAttribute = "Uno.Extensions.Reactive.Bindings.ModelAttribute";
	private const string HotTesting = "global::Uno.HotTesting.Reactive";

	public void Initialize(GeneratorInitializationContext context) { }

	public void Execute(GeneratorExecutionContext context)
	{
		var compilation = context.Compilation;
		var feedDep = compilation.GetTypeByMetadataName(FeedDependencyAttribute);
		var modelAttr = compilation.GetTypeByMetadataName(ModelAttribute);
		if (feedDep is null || modelAttr is null)
		{
			return; // Core not referenced → nothing to do.
		}

		foreach (var model in EnumerateModels(compilation, feedDep))
		{
			if (GenerateFor(model, feedDep, modelAttr) is { } generated)
			{
				context.AddSource($"{model.ToDisplayString().Replace('.', '_')}.Mock.g.cs", generated);
			}
		}
	}

	private static IEnumerable<INamedTypeSymbol> EnumerateModels(Compilation compilation, INamedTypeSymbol feedDep)
	{
		bool HasFeedDep(INamedTypeSymbol t)
			=> t.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, feedDep));

		IEnumerable<INamedTypeSymbol> Walk(INamespaceOrTypeSymbol ns)
		{
			foreach (var member in ns.GetMembers())
			{
				if (member is INamespaceSymbol childNs)
				{
					foreach (var t in Walk(childNs)) yield return t;
				}
				else if (member is INamedTypeSymbol type)
				{
					if (HasFeedDep(type)) yield return type;
					foreach (var nested in type.GetTypeMembers())
					{
						if (HasFeedDep(nested)) yield return nested;
					}
				}
			}
		}

		foreach (var t in Walk(compilation.Assembly.GlobalNamespace)) yield return t;

		foreach (var reference in compilation.References)
		{
			if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol asm)
			{
				foreach (var t in Walk(asm.GlobalNamespace)) yield return t;
			}
		}
	}

	private sealed class FeedMember
	{
		public string Name = "";
		public string FeedTypeFullName = "";
		public string ItemOrValueFullName = "";
		public bool IsList;
	}

	private string? GenerateFor(INamedTypeSymbol model, INamedTypeSymbol feedDep, INamedTypeSymbol modelAttr)
	{
		var modelAttrData = model.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, modelAttr));
		if (modelAttrData?.ConstructorArguments is not { Length: 1 } args || args[0].Value is not INamedTypeSymbol vm)
		{
			return null;
		}

		var inputs = new List<FeedMember>();   // OnParameter set
		var derived = new List<FeedMember>();  // OnFeed set

		foreach (var attr in model.GetAttributes().Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, feedDep)))
		{
			if (attr.ConstructorArguments is not { Length: 1 } ca || ca[0].Value is not string memberName)
			{
				continue;
			}

			var onParameter = attr.NamedArguments.FirstOrDefault(n => n.Key == "OnParameter").Value.Value as string;
			var onFeed = attr.NamedArguments.FirstOrDefault(n => n.Key == "OnFeed").Value.Value as string;
			if (onParameter is null && onFeed is null)
			{
				continue; // independent → not part of the mock
			}

			if (model.GetMembers(memberName).FirstOrDefault() is not { } memberSymbol)
			{
				continue;
			}

			var memberType = memberSymbol switch
			{
				IPropertySymbol p => p.Type,
				IFieldSymbol f => f.Type,
				_ => null,
			};
			if (memberType is null || !TryGetFeed(memberType, out var isList, out var valueType))
			{
				continue;
			}

			var fm = new FeedMember
			{
				Name = memberName,
				FeedTypeFullName = memberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				ItemOrValueFullName = valueType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				IsList = isList,
			};

			(onFeed is not null ? derived : inputs).Add(fm);
		}

		// Commands: the generated VM exposes them as public IAsyncCommand properties, overridable via
		// the __Mock_SetCommand seam (emitted by the MVUX generator).
		var commands = vm.GetMembers()
			.OfType<IPropertySymbol>()
			.Where(pr => !pr.IsStatic && pr.DeclaredAccessibility == Accessibility.Public
				&& pr.Type.ToDisplayString() == "Uno.Extensions.Reactive.IAsyncCommand")
			.Select(pr => pr.Name)
			.ToList();
		if (!vm.GetMembers("__Mock_SetCommand").Any())
		{
			commands.Clear();
		}

		if (inputs.Count == 0 && derived.Count == 0 && commands.Count == 0)
		{
			return null;
		}

		var vmFull = vm.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		var mockName = $"{model.Name}Mock";
		var vmMockName = $"{vm.Name}Mock";
		var ns = model.ContainingNamespace.IsGlobalNamespace ? null : model.ContainingNamespace.ToDisplayString();

		// Record members.
		var recordMembers = new StringBuilder();
		foreach (var m in inputs)
		{
			recordMembers.AppendLine($"\tpublic required {m.FeedTypeFullName} {m.Name} {{ get; init; }}");
		}
		foreach (var m in derived)
		{
			recordMembers.AppendLine($"\tpublic {m.FeedTypeFullName}? {m.Name} {{ get; init; }}");
		}
		foreach (var c in commands)
		{
			recordMembers.AppendLine($"\tpublic global::Uno.Extensions.Reactive.IAsyncCommand? {c} {{ get; init; }}");
		}

		// Empty initializer + Create(inputs) params/inits.
		var emptyInits = string.Join(", ", inputs.Select(m => m.IsList
			? $"{m.Name} = {HotTesting}.ListFeedMock.Empty<{m.ItemOrValueFullName}>()"
			: $"{m.Name} = {HotTesting}.FeedMock.Empty<{m.ItemOrValueFullName}>()"));
		var createParams = string.Join(", ", inputs.Select(m => $"{m.FeedTypeFullName} {Camel(m.Name)}"));
		var createInits = string.Join(", ", inputs.Select(m => $"{m.Name} = {Camel(m.Name)}"));

		// Empty state lives on the record so it composes with `with` (spec §8).
		recordMembers.AppendLine();
		recordMembers.AppendLine($"\tpublic static {mockName} Empty {{ get; }} = new() {{ {emptyInits} }};");

		// SetModel body.
		var setBody = new StringBuilder();
		foreach (var m in inputs)
		{
			var swap = m.IsList ? "SwapListFeed" : "SwapFeed";
			setBody.AppendLine($"\t\t{HotTesting}.MockingService.{swap}<{m.ItemOrValueFullName}>(model, model.{m.Name}, mock.{m.Name});");
		}
		foreach (var m in derived)
		{
			var swap = m.IsList ? "SwapListFeed" : "SwapFeed";
			setBody.AppendLine($"\t\tif (mock.{m.Name} is not null)");
			setBody.AppendLine($"\t\t\t{HotTesting}.MockingService.{swap}<{m.ItemOrValueFullName}>(model, model.{m.Name}, mock.{m.Name});");
		}
		foreach (var c in commands)
		{
			setBody.AppendLine($"\t\tif (mock.{c} is not null)");
			setBody.AppendLine($"\t\t\tvm.__Mock_SetCommand(\"{c}\", mock.{c});");
		}

		var nsHeader = ns is null ? "" : $"namespace {ns};\n\n";
		var createFromInputs = inputs.Count == 0
			? ""
			: $$"""

				public static {{vmFull}} Create({{createParams}})
					=> Create(new {{mockName}} { {{createInits}} });
			""";

		return $$"""
			// <auto-generated />
			#nullable enable
			{{nsHeader}}public sealed record {{mockName}}
			{
			{{recordMembers.ToString().TrimEnd()}}
			}

			public static class {{vmMockName}}
			{
				public static {{vmFull}} Create() => Create({{mockName}}.Empty);
			{{createFromInputs}}
				public static {{vmFull}} Create({{mockName}} mock)
				{
					var vm = new {{vmFull}}(default!);
					vm.SetModel(mock);
					return vm;
				}

				public static void SetModel(this {{vmFull}} vm, {{mockName}} mock)
				{
					var model = vm.Model;
			{{setBody.ToString().TrimEnd()}}
				}
			}

			""";
	}

	private static bool TryGetFeed(ITypeSymbol type, out bool isList, out ITypeSymbol? valueType)
	{
		isList = false;
		valueType = null;
		foreach (var intf in type.AllInterfaces.Concat(type is INamedTypeSymbol nt ? new[] { nt } : Array.Empty<INamedTypeSymbol>()))
		{
			if (intf.OriginalDefinition.MetadataName == "IListFeed`1")
			{
				isList = true;
				valueType = intf.TypeArguments.FirstOrDefault();
				return valueType is not null;
			}
		}
		foreach (var intf in type.AllInterfaces.Concat(type is INamedTypeSymbol nt2 ? new[] { nt2 } : Array.Empty<INamedTypeSymbol>()))
		{
			if (intf.OriginalDefinition.MetadataName == "IFeed`1")
			{
				isList = false;
				valueType = intf.TypeArguments.FirstOrDefault();
				return valueType is not null;
			}
		}
		return false;
	}

	private static string Camel(string name)
		=> string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name.Substring(1);
}
