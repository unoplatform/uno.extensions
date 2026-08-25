using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Mocking.Generator;

/// <summary>
/// Consumer-side generator (spec 013, tiers 2/3). Runs in a test/preview project, reads the app
/// metadata (models carrying <c>FeedDependency</c>/<c>CtorDependency</c> attributes + their generated
/// view-models) and emits, per model:
///   - <c>record {Model}Mock</c> — required service-dependent inputs, optional derived overrides;
///   - <c>{Vm}.Create(...)</c> — null-inject construction (under the ambient MockingService scope);
///   - <c>SetModel(this {Vm}, {Model}Mock)</c> — typed swaps via the <c>MockModel</c> reflection engine.
/// Strongly typed end to end (D9); no tier-1 surface. Commands and the zero-arg <c>Create()</c>/<c>Empty</c>
/// come in a later increment.
/// </summary>
[Generator]
public sealed class FeedsMockGenerator : ISourceGenerator
{
	private const string FeedDependencyAttribute = "Uno.Extensions.Reactive.Config.FeedDependencyAttribute";
	private const string CtorDependencyAttribute = "Uno.Extensions.Reactive.Config.CtorDependencyAttribute";
	private const string ModelAttribute = "Uno.Extensions.Reactive.Bindings.ModelAttribute";
	private const string FeedInterface = "Uno.Extensions.Reactive.IFeed`1";
	private const string ListFeedInterface = "Uno.Extensions.Reactive.IListFeed`1";

	public void Initialize(GeneratorInitializationContext context) { }

	public void Execute(GeneratorExecutionContext context)
	{
		var compilation = context.Compilation;
		var feedDepSymbol = compilation.GetTypeByMetadataName(FeedDependencyAttribute);
		var ctorDepSymbol = compilation.GetTypeByMetadataName(CtorDependencyAttribute);
		var modelAttrSymbol = compilation.GetTypeByMetadataName(ModelAttribute);
		if (feedDepSymbol is null || modelAttrSymbol is null)
		{
			return; // Core not referenced → nothing to do.
		}

		foreach (var model in EnumerateModels(compilation, feedDepSymbol))
		{
			if (GenerateFor(model, feedDepSymbol, ctorDepSymbol, modelAttrSymbol) is { } generated)
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

		// Current compilation.
		foreach (var t in Walk(compilation.Assembly.GlobalNamespace)) yield return t;

		// Referenced assemblies (the app).
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
		public string FeedTypeFullName = "";   // e.g. global::Uno...IListFeed<global::App.Step>
		public string ItemOrValueFullName = ""; // T
		public bool IsList;
		public bool IsDerived;                  // OnFeed set → optional override
	}

	private string? GenerateFor(INamedTypeSymbol model, INamedTypeSymbol feedDep, INamedTypeSymbol? ctorDep, INamedTypeSymbol modelAttr)
	{
		// Resolve the generated view-model via [Model(typeof(Vm))].
		var modelAttrData = model.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, modelAttr));
		if (modelAttrData?.ConstructorArguments is not { Length: 1 } args || args[0].Value is not INamedTypeSymbol vm)
		{
			return null;
		}

		// Classify members from FeedDependency attributes.
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
				IsDerived = onFeed is not null,
			};

			(onFeed is not null ? derived : inputs).Add(fm);
		}

		if (inputs.Count == 0 && derived.Count == 0)
		{
			return null;
		}

		var vmFull = vm.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		var modelFull = model.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		var mockName = $"{model.Name}Mock";
		var ns = model.ContainingNamespace.IsGlobalNamespace ? null : model.ContainingNamespace.ToDisplayString();

		var sb = new StringBuilder();
		sb.AppendLine("// <auto-generated />");
		sb.AppendLine("#nullable enable");
		if (ns is not null)
		{
			sb.AppendLine($"namespace {ns};");
			sb.AppendLine();
		}

		// The mock record.
		sb.AppendLine($"public sealed record {mockName}");
		sb.AppendLine("{");
		foreach (var m in inputs)
		{
			sb.AppendLine($"\tpublic required {m.FeedTypeFullName} {m.Name} {{ get; init; }}");
		}
		foreach (var m in derived)
		{
			sb.AppendLine($"\tpublic {m.FeedTypeFullName}? {m.Name} {{ get; init; }}");
		}
		sb.AppendLine("}");
		sb.AppendLine();

		// The factory + facade.
		sb.AppendLine($"public static class {mockName}Extensions");
		sb.AppendLine("{");

		// Create(inputs...) — required inputs as parameters.
		var createParams = string.Join(", ", inputs.Select(m => $"{m.FeedTypeFullName} {Camel(m.Name)}"));
		var mockInit = string.Join(", ", inputs.Select(m => $"{m.Name} = {Camel(m.Name)}"));
		sb.AppendLine($"\tpublic static {vmFull} Create({createParams})");
		sb.AppendLine($"\t\t=> Create(new {mockName} {{ {mockInit} }});");
		sb.AppendLine();

		// Create(mock) — null-inject construction + SetModel.
		sb.AppendLine($"\tpublic static {vmFull} Create({mockName} mock)");
		sb.AppendLine("\t{");
		sb.AppendLine($"\t\tvar vm = new {vmFull}(default!);");
		sb.AppendLine("\t\tvm.SetModel(mock);");
		sb.AppendLine("\t\treturn vm;");
		sb.AppendLine("\t}");
		sb.AppendLine();

		// SetModel — typed swaps via the reflection engine.
		sb.AppendLine($"\tpublic static void SetModel(this {vmFull} vm, {mockName} mock)");
		sb.AppendLine("\t{");
		sb.AppendLine($"\t\tvar model = vm.Model;");
		foreach (var m in inputs)
		{
			var swap = m.IsList ? "SwapListFeed" : "SwapFeed";
			sb.AppendLine($"\t\tglobal::Uno.Extensions.Reactive.Mocking.MockModel.{swap}<{m.ItemOrValueFullName}>(model, model.{m.Name}, mock.{m.Name});");
		}
		foreach (var m in derived)
		{
			var swap = m.IsList ? "SwapListFeed" : "SwapFeed";
			sb.AppendLine($"\t\tif (mock.{m.Name} is not null)");
			sb.AppendLine($"\t\t\tglobal::Uno.Extensions.Reactive.Mocking.MockModel.{swap}<{m.ItemOrValueFullName}>(model, model.{m.Name}, mock.{m.Name});");
		}
		sb.AppendLine("\t}");
		sb.AppendLine("}");

		return sb.ToString();
	}

	private static bool TryGetFeed(ITypeSymbol type, out bool isList, out ITypeSymbol? valueType)
	{
		isList = false;
		valueType = null;
		foreach (var intf in type.AllInterfaces.Concat(type is INamedTypeSymbol nt ? new[] { nt } : Array.Empty<INamedTypeSymbol>()))
		{
			var def = intf.OriginalDefinition.ToDisplayString();
			if (def == "Uno.Extensions.Reactive.IListFeed<T>" || intf.OriginalDefinition.MetadataName == "IListFeed`1")
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
