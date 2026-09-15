using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;
using Uno.Extensions.Reactive.Generator;

namespace Uno.HotTesting.Reactive.Generator;

/// <summary>
/// Consumer-side generator (spec 013, tiers 2/3). Reads the MVUX models reachable from the compilation
/// and emits, per model:
///   - <c>record {Model}Mock</c> — required service-dependent inputs, optional derived + command overrides;
///   - <c>{Vm}.Create(...)</c> — null-inject construction (under the ambient MockingService scope);
///   - <c>SetModel(this {Vm}, {Model}Mock)</c> — strongly-typed swaps via <c>MockingService</c>.
/// Strongly typed end to end (D9); reuses the <c>Uno.HotTesting.Reactive</c> vocabulary (FeedMock /
/// ListFeedMock / CommandMock).
///
/// Models are reached two ways, because the MVUX metadata is only readable when it is already compiled:
///   - <b>referenced assemblies</b> (a test/preview project referencing the app) — the
///     <c>FeedDependency</c>/<c>Model</c> attributes are metadata, so they are read directly;
///   - <b>the current compilation</b> (a single-project app referencing this package itself) — those
///     attributes are emitted by a sibling generator and a generator cannot observe another
///     generator's output, so the shared <see cref="FeedMockingAnalysis"/> is run over the source
///     instead. The view-model does not exist as a symbol there either; it is named and constructed
///     from the model, which is what the MVUX generator derives it from.
/// </summary>
[Generator]
public sealed class FeedsMockGenerator : ISourceGenerator
{
	private const string FeedDependencyAttribute = "Uno.Extensions.Reactive.Config.FeedDependencyAttribute";
	private const string ModelAttribute = "Uno.Extensions.Reactive.Bindings.ModelAttribute";
	private const string ImplicitBindablesAttribute = "ImplicitBindablesAttribute";
	private const string ReactiveBindableAttribute = "ReactiveBindableAttribute";
	private const string DefaultModelPattern = "Model$";
	private const string ViewModelSuffix = "ViewModel";
	private const string HotTesting = "global::Uno.HotTesting.Reactive";

	// MOCK0001: a reachable model whose view-model Create cannot build. Reported, never silently skipped.
	private static readonly DiagnosticDescriptor NoPublicConstructor = new DiagnosticDescriptor(
		"MOCK0001",
		"Mock not generated",
		"No mock is generated for the model '{0}': its view-model '{1}' exposes no public constructor for Create to null-inject",
		"Usage",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: "https://platform.uno/docs/articles/external/uno.extensions/doc/Reference/Reactive/rules.html#Mock0001");

	/// <inheritdoc />
	public void Initialize(GeneratorInitializationContext context) { }

	/// <inheritdoc />
	public void Execute(GeneratorExecutionContext context)
	{
		var compilation = context.Compilation;
		var feedDep = compilation.GetTypeByMetadataName(FeedDependencyAttribute);
		var modelAttr = compilation.GetTypeByMetadataName(ModelAttribute);
		if (feedDep is null || modelAttr is null)
		{
			return; // Core not referenced → nothing to do.
		}

		var emitted = new HashSet<string>(StringComparer.Ordinal);

		// Declared metadata first, so a hand-written [FeedDependency]/[Model] wins over the inferred
		// classification, as the spec's explicit-declaration escape hatch requires.
		foreach (var model in EnumerateAttributedModels(compilation, feedDep))
		{
			// The app already generated its own mocks (it references this package too): emitting a second
			// copy here would put the same type name in the same namespace in two assemblies.
			if (compilation.GetTypeByMetadataName(MockMetadataName(model)) is not null)
			{
				continue;
			}

			if (DescribeFromMetadata(model, feedDep, modelAttr) is { } described)
			{
				AddSource(context, described, emitted);
			}
		}

		var analysis = new FeedMockingAnalysis(compilation, IsFeedMember);
		foreach (var model in EnumerateSourceModels(compilation))
		{
			if (DescribeFromSource(model, analysis) is { } described)
			{
				AddSource(context, described, emitted);
			}
		}
	}

	private static void AddSource(GeneratorExecutionContext context, ModelMock described, HashSet<string> emitted)
	{
		var fileName = $"{described.Model.ToDisplayString().Replace('.', '_')}.Mock.g.cs";
		if (!emitted.Add(fileName))
		{
			return;
		}

		if (Generate(context, described) is { } generated)
		{
			context.AddSource(fileName, generated);
		}
	}

	private static string MockMetadataName(INamedTypeSymbol model)
		=> model.ContainingNamespace.IsGlobalNamespace
			? $"{model.Name}Mock"
			: $"{model.ContainingNamespace.ToDisplayString()}.{model.Name}Mock";

	private static bool IsFeedMember(ISymbol member)
	{
		var type = member switch
		{
			IPropertySymbol p => p.Type,
			IFieldSymbol f => f.Type,
			_ => null,
		};
		return type is not null && TryGetFeed(type, out _, out _);
	}

	/// <summary>
	/// Models carrying readable MVUX metadata: every model of a referenced assembly, plus any model of
	/// this compilation whose attributes were hand-declared (a generated one is not observable here).
	/// </summary>
	private static IEnumerable<INamedTypeSymbol> EnumerateAttributedModels(Compilation compilation, INamedTypeSymbol feedDep)
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
					foreach (var nested in type.GetTypeMembers().Where(HasFeedDep))
					{
						yield return nested;
					}
				}
			}
		}

		foreach (var t in Walk(compilation.Assembly.GlobalNamespace)) yield return t;

		foreach (var asm in compilation.References.Select(compilation.GetAssemblyOrModuleSymbol).OfType<IAssemblySymbol>())
		{
			foreach (var t in Walk(asm.GlobalNamespace)) yield return t;
		}
	}

	/// <summary>
	/// Models declared in the compilation being generated. The MVUX metadata is not readable here, so
	/// this mirrors how the MVUX generator decides a type is a model: an explicit
	/// <c>[ReactiveBindable]</c>, or a partial type whose full name matches the assembly's implicit
	/// patterns (<c>Model$</c> unless overridden).
	/// </summary>
	private static IEnumerable<INamedTypeSymbol> EnumerateSourceModels(Compilation compilation)
	{
		var (implicitEnabled, patterns) = ReadImplicitBindables(compilation.Assembly);

		bool IsModel(INamedTypeSymbol type)
		{
			if (ReadReactiveBindable(type) is { } explicitlyEnabled)
			{
				// When the attribute is set the `partial` is not checked: the build must fail if it is missing.
				return explicitlyEnabled;
			}

			return type.IsPartial()
				&& implicitEnabled
				&& patterns.Any(pattern => Regex.IsMatch(type.ToString(), pattern));
		}

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
					if (IsModel(type)) yield return type;
					foreach (var nested in type.GetTypeMembers().Where(IsModel))
					{
						yield return nested;
					}
				}
			}
		}

		return Walk(compilation.Assembly.GlobalNamespace);
	}

	private static (bool isEnabled, string[] patterns) ReadImplicitBindables(IAssemblySymbol assembly)
	{
		var attribute = assembly
			.GetAttributes()
			.FirstOrDefault(a => a.AttributeClass?.Name == ImplicitBindablesAttribute);
		if (attribute is null)
		{
			return (true, new[] { DefaultModelPattern });
		}

		var isEnabled = attribute.NamedArguments.FirstOrDefault(na => na.Key == "IsEnabled").Value.Value as bool? ?? true;

		var patterns = attribute.ConstructorArguments
			.SelectMany(arg => arg.Kind == TypedConstantKind.Array ? (IEnumerable<TypedConstant>)arg.Values : new[] { arg })
			.Select(value => value.Value as string)
			.Where(value => !string.IsNullOrEmpty(value))
			.Select(value => value!)
			.ToArray();

		return (isEnabled, patterns.Length > 0 ? patterns : new[] { DefaultModelPattern });
	}

	private static bool? ReadReactiveBindable(INamedTypeSymbol type)
	{
		var attribute = type
			.GetAttributes()
			.FirstOrDefault(a => a.AttributeClass?.Name == ReactiveBindableAttribute);
		if (attribute is null)
		{
			return null;
		}

		return attribute.ConstructorArguments.FirstOrDefault().Value as bool?
			?? attribute.NamedArguments.FirstOrDefault(na => na.Key == "IsEnabled").Value.Value as bool?
			?? true;
	}

	private sealed class FeedMember
	{
		public string Name = "";
		public string FeedTypeFullName = "";
		public string ItemOrValueFullName = "";
		public bool IsList;
	}

	/// <summary>Everything the emission needs, however the model was reached.</summary>
	private sealed class ModelMock
	{
		public INamedTypeSymbol Model = null!;
		public string ViewModelName = "";
		public string ViewModelFullName = "";

		/// <summary>
		/// The constructor <c>Create</c> null-injects. On the source path this is the model's own
		/// constructor: the generated view-model mirrors it.
		/// </summary>
		public IMethodSymbol? Constructor;

		public List<FeedMember> Inputs = new();
		public List<FeedMember> Derived = new();
	}

	private static ModelMock? DescribeFromMetadata(INamedTypeSymbol model, INamedTypeSymbol feedDep, INamedTypeSymbol modelAttr)
	{
		var modelAttrData = model.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, modelAttr));
		if (modelAttrData?.ConstructorArguments is not { Length: 1 } args || args[0].Value is not INamedTypeSymbol vm)
		{
			return null;
		}

		var described = new ModelMock
		{
			Model = model,
			ViewModelName = vm.Name,
			ViewModelFullName = vm.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			Constructor = PickConstructor(vm.Constructors),
		};

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

			if (ToFeedMember(memberSymbol) is not { } fm)
			{
				continue;
			}

			(onFeed is not null ? described.Derived : described.Inputs).Add(fm);
		}

		return described;
	}

	private static ModelMock? DescribeFromSource(INamedTypeSymbol model, FeedMockingAnalysis analysis)
	{
		var feedMembers = analysis.GetFeedMembers(model);
		if (feedMembers.Count == 0)
		{
			return null;
		}

		var modelName = model.Name.TrimEnd("Model", StringComparison.Ordinal);
		var viewModelName = $"{modelName}{ViewModelSuffix}";
		var ns = model.ContainingNamespace.IsGlobalNamespace ? null : model.ContainingNamespace.ToDisplayString();

		var described = new ModelMock
		{
			Model = model,
			ViewModelName = viewModelName,
			ViewModelFullName = ns is null ? $"global::{viewModelName}" : $"global::{ns}.{viewModelName}",
			// The generated view-model mirrors the model's constructors, so selecting here is equivalent.
			Constructor = PickConstructor(analysis.AccessibleInstanceCtors(model).ToArray()),
		};

		var feedMemberNames = new HashSet<string>(feedMembers.Select(m => m.Name), StringComparer.Ordinal);
		var ctorParamNames = analysis.GetCtorParameterNames(model);
		var fieldToParam = analysis.BuildFieldToParamMap(model, ctorParamNames);

		foreach (var member in feedMembers)
		{
			var (kind, _, _) = analysis.ClassifyFeedMember(member, feedMemberNames, ctorParamNames, fieldToParam, model);
			if (kind == FeedKind.Independent)
			{
				continue; // not part of the mock
			}

			if (ToFeedMember(member) is not { } fm)
			{
				continue;
			}

			(kind == FeedKind.Derived ? described.Derived : described.Inputs).Add(fm);
		}

		return described;
	}

	private static FeedMember? ToFeedMember(ISymbol memberSymbol)
	{
		var memberType = memberSymbol switch
		{
			IPropertySymbol p => p.Type,
			IFieldSymbol f => f.Type,
			_ => null,
		};
		if (memberType is null || !TryGetFeed(memberType, out var isList, out var valueType))
		{
			return null;
		}

		return new FeedMember
		{
			Name = memberSymbol.Name,
			FeedTypeFullName = memberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			ItemOrValueFullName = valueType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			IsList = isList,
		};
	}

	/// <summary>
	/// Create null-injects the public constructor with the fewest parameters. The generated view-model
	/// mirrors the model's constructors; its protected model-wrapping constructor is never a candidate.
	/// Ties on arity are broken on the parameter type list so the emitted code does not depend on symbol order.
	/// </summary>
	private static IMethodSymbol? PickConstructor(IReadOnlyCollection<IMethodSymbol> constructors)
		=> constructors
			.Where(c => !c.IsStatic && c.DeclaredAccessibility == Accessibility.Public)
			.OrderBy(c => c.Parameters.Length)
			.ThenBy(ParameterTypes, StringComparer.Ordinal)
			.FirstOrDefault();

	private static string? Generate(GeneratorExecutionContext context, ModelMock described)
	{
		var model = described.Model;

		// Command mocking is deferred to vNext: the consumer generator emits no command overrides for
		// now (the MVUX __Mock_SetCommand seam stays available for that future work).

		if (described.Inputs.Count == 0 && described.Derived.Count == 0)
		{
			return null;
		}

		if (described.Constructor is not { } ctor)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				NoPublicConstructor,
				model.Locations.FirstOrDefault(location => location.IsInSource) ?? Location.None,
				model.Name,
				described.ViewModelName));
			return null;
		}

		// Typed defaults: a bare `default!` cannot pick between constructors of equal arity (CS0121).
		var ctorArguments = string.Join(", ", ctor.Parameters.Select(p => $"default({FullName(p.Type)})! /* {p.Name} */"));
		var vmFull = described.ViewModelFullName;
		var mockName = $"{model.Name}Mock";
		var vmMockName = $"{described.ViewModelName}Mock";
		var ns = model.ContainingNamespace.IsGlobalNamespace ? null : model.ContainingNamespace.ToDisplayString();

		// Record members.
		var recordMembers = new StringBuilder();
		foreach (var m in described.Inputs)
		{
			recordMembers.AppendLine($"\tpublic required {m.FeedTypeFullName} {m.Name} {{ get; init; }}");
		}
		foreach (var m in described.Derived)
		{
			recordMembers.AppendLine($"\tpublic {m.FeedTypeFullName}? {m.Name} {{ get; init; }}");
		}

		// Empty initializer + Create(inputs) params/inits.
		var emptyInits = string.Join(", ", described.Inputs.Select(m => m.IsList
			? $"{m.Name} = {HotTesting}.ListFeedMock.Empty<{m.ItemOrValueFullName}>()"
			: $"{m.Name} = {HotTesting}.FeedMock.Empty<{m.ItemOrValueFullName}>()"));

		// Empty state lives on the record so it composes with `with` (spec §8).
		recordMembers.AppendLine();
		recordMembers.AppendLine($"\tpublic static {mockName} Empty {{ get; }} = new() {{ {emptyInits} }};");

		// SetModel body.
		var setBody = new StringBuilder();
		foreach (var m in described.Inputs)
		{
			var swap = m.IsList ? "SwapListFeed" : "SwapFeed";
			setBody.AppendLine($"\t\t{HotTesting}.MockingService.{swap}<{m.ItemOrValueFullName}>(model, model.{m.Name}, mock.{m.Name});");
		}
		foreach (var m in described.Derived)
		{
			var swap = m.IsList ? "SwapListFeed" : "SwapFeed";
			setBody.AppendLine($"\t\tif (mock.{m.Name} is not null)");
			setBody.AppendLine($"\t\t\t{HotTesting}.MockingService.{swap}<{m.ItemOrValueFullName}>(model, model.{m.Name}, mock.{m.Name});");
		}

		var nsHeader = ns is null ? "" : $"namespace {ns};\n\n";
		return $$"""
			// <auto-generated />
			#nullable enable
			{{nsHeader}}public sealed record {{mockName}}
			{
			{{recordMembers.ToString().TrimEnd()}}
			}

			public static partial class {{vmMockName}}
			{
				public static {{vmFull}} Create() => Create({{mockName}}.Empty);

				public static {{vmFull}} Create({{mockName}} mock)
				{
					// The activation scope is only needed while the VM/Model context is created:
					// the mockable bit is captured on that context instance, so later SetMock calls
					// (and lazy first subscriptions) still swap even after the scope is disposed.
					using (global::Uno.HotTesting.Reactive.MockingService.Enable())
					{
						var vm = new {{vmFull}}({{ctorArguments}});
						vm.SetMock(mock);
						return vm;
					}
				}

				public static void SetMock(this {{vmFull}} vm, {{mockName}} mock)
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

	private static string ParameterTypes(IMethodSymbol ctor)
		=> string.Join(",", ctor.Parameters.Select(p => FullName(p.Type)));

	private static string FullName(ITypeSymbol type)
		=> type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}
