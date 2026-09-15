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
///   - <c>SetMock(this {Vm}, {Model}Mock)</c> — strongly-typed swaps via <c>MockingService</c>.
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
	private const string EnableFeedMockingAttribute = "EnableFeedMockingAttribute";
	private const string ReactiveAssemblyName = "Uno.Extensions.Reactive";
	private const string DefaultModelPattern = "Model$";
	private const string ViewModelSuffix = "ViewModel";
	private const string HotTesting = "global::Uno.HotTesting.Reactive";

	private static readonly Regex InvalidHintNameCharacters = new Regex("[^A-Za-z0-9_.]", RegexOptions.CultureInvariant);
	private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(1);

	// MOCK0001: a reachable model whose view-model Create cannot build. Reported, never silently skipped.
	private static readonly DiagnosticDescriptor NoPublicConstructor = new DiagnosticDescriptor(
		"MOCK0001",
		"Mock not generated",
		"No mock is generated for the model '{0}': its view-model '{1}' exposes no public constructor for Create to null-inject",
		"Usage",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: "https://platform.uno/docs/articles/external/uno.extensions/doc/Reference/Reactive/rules.html#Mock0001");

	// MOCK0002: a model was considered but carries no feed a mock could drive. Info, so it informs without
	// failing a build whose models legitimately take no service.
	private static readonly DiagnosticDescriptor NoMockableInput = new DiagnosticDescriptor(
		"MOCK0002",
		"Mock not generated",
		"No mock is generated for the model '{0}': none of its feeds is fed by a constructor parameter or derived from one",
		"Usage",
		DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: "https://platform.uno/docs/articles/external/uno.extensions/doc/Reference/Reactive/rules.html#Mock0002");

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

		if (IsMockingDisabled(compilation))
		{
			// The same opt-out the MVUX generator honours: without it the metadata it emits disappears but
			// the mocks would not, leaving swaps pointed at a seam that was never generated.
			return;
		}

		var emitted = new HashSet<string>(StringComparer.Ordinal);

		// Declared metadata first, so a hand-written [FeedDependency]/[Model] wins over the inferred
		// classification, as the spec's explicit-declaration escape hatch requires.
		foreach (var model in EnumerateAttributedModels(compilation, feedDep))
		{
			if (IsAlreadyDeclared(compilation, model))
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
			if (IsAlreadyDeclared(compilation, model))
			{
				continue;
			}

			if (DescribeFromSource(model, analysis) is { } described)
			{
				AddSource(context, described, emitted);
			}
		}
	}

	/// <summary>
	/// The mock type already exists — generated in a referenced assembly that also has this package, or
	/// hand-written. Emitting a second one would collide (CS0101) or be ambiguous at the use site.
	/// </summary>
	private static bool IsAlreadyDeclared(Compilation compilation, INamedTypeSymbol model)
		=> compilation.GetTypeByMetadataName(MockMetadataName(model)) is not null;

	private static bool IsMockingDisabled(Compilation compilation)
		=> compilation.Assembly
			.GetAttributes()
			.Any(a => a.AttributeClass?.Name == EnableFeedMockingAttribute
				&& (a.NamedArguments.FirstOrDefault(na => na.Key == "IsEnabled").Value.Value as bool?) == false);

	private static void AddSource(GeneratorExecutionContext context, ModelMock described, HashSet<string> emitted)
	{
		// Keyed on the model, and only once it actually produced something: a model the metadata path
		// could not describe must stay available to the source path.
		var key = described.Model.ToDisplayString();
		if (emitted.Contains(key))
		{
			return;
		}

		if (Generate(context, described) is not { } generated)
		{
			return;
		}

		emitted.Add(key);

		// A generic model's display string carries characters Roslyn rejects in a hint name, and it
		// rejects them by throwing — which would drop every remaining mock, not just this one.
		var fileName = InvalidHintNameCharacters.Replace(key.Replace('.', '_'), "_") + ".Mock.g.cs";
		context.AddSource(fileName, generated);
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

		// A model carries attributes defined by Uno.Extensions.Reactive, so an assembly that does not
		// reference it cannot declare one. Skipping those avoids realizing the whole metadata closure
		// (the framework and every transitive package) on each compilation.
		foreach (var asm in compilation.References
			.Select(compilation.GetAssemblyOrModuleSymbol)
			.OfType<IAssemblySymbol>()
			.Where(MayDeclareModels))
		{
			foreach (var t in Walk(asm.GlobalNamespace)) yield return t;
		}
	}

	private static bool MayDeclareModels(IAssemblySymbol assembly)
		=> assembly.Modules.Any(module => module.ReferencedAssemblies.Any(reference => reference.Name == ReactiveAssemblyName));

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
			// A nested model's view-model is generated nested inside its containing partial, so its name
			// cannot be derived from the namespace the way a top-level one can. Left to the metadata path.
			if (type.ContainingType is not null)
			{
				return false;
			}

			if (ReadReactiveBindable(type) is { } explicitlyEnabled)
			{
				// When the attribute is set the `partial` is not checked: the build must fail if it is missing.
				return explicitlyEnabled;
			}

			return type.IsPartial()
				&& implicitEnabled
				&& patterns.Any(pattern => IsMatch(pattern, type.ToString()));
		}

		IEnumerable<INamedTypeSymbol> Walk(INamespaceOrTypeSymbol ns)
		{
			foreach (var member in ns.GetMembers())
			{
				if (member is INamespaceSymbol childNs)
				{
					foreach (var t in Walk(childNs)) yield return t;
				}
				else if (member is INamedTypeSymbol type && IsModel(type))
				{
					yield return type;
				}
			}
		}

		return Walk(compilation.Assembly.GlobalNamespace);
	}

	/// <summary>
	/// The patterns come from an attribute in user code. A malformed one must not take the whole
	/// generator down with it (every mock would vanish behind a CS8785), and a pathological one must not
	/// hang the compiler, so matching is bounded and failures simply do not match.
	/// </summary>
	private static bool IsMatch(string pattern, string typeName)
	{
		try
		{
			return Regex.IsMatch(typeName, pattern, RegexOptions.CultureInvariant, RegexMatchTimeout);
		}
		catch (ArgumentException)
		{
			return false; // Not a valid expression — the MVUX generator reports on the attribute itself.
		}
		catch (RegexMatchTimeoutException)
		{
			return false;
		}
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

	/// <summary>Reads <c>[ReactiveBindable]</c> off a type or a constructor; null when it is absent.</summary>
	private static bool? ReadReactiveBindable(ISymbol symbol)
	{
		var attribute = symbol
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
		/// What the emitted types are declared as. The generated view-model carries the model's own
		/// accessibility, so a mock more visible than it would not compile (CS0050/CS0051).
		/// </summary>
		public string Accessibility = "public";

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
			Accessibility = model.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Public ? "public" : "internal",
			// The generated view-model mirrors the model's constructors, so selecting here is equivalent.
			// Internal ones count on this path: the mock is emitted into the model's own assembly.
			Constructor = PickConstructor(analysis.AccessibleInstanceCtors(model).ToArray(), includeInternal: true),
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
		if (memberType is null || !TryGetFeed(memberType, out var isList, out var feedInterface))
		{
			return null;
		}

		return new FeedMember
		{
			Name = memberSymbol.Name,
			FeedTypeFullName = feedInterface!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			ItemOrValueFullName = feedInterface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			IsList = isList,
		};
	}

	/// <summary>
	/// Create null-injects the constructor with the fewest parameters. The generated view-model
	/// mirrors the model's constructors; its protected model-wrapping constructor is never a candidate.
	/// Ties on arity are broken on the parameter type list so the emitted code does not depend on symbol order.
	/// A constructor the MVUX generator was told to skip is not mirrored onto the view-model, so it is not
	/// a candidate here either.
	/// </summary>
	/// <param name="constructors">The candidate constructors, already filtered of the clone constructor.</param>
	/// <param name="includeInternal">
	/// Whether an internal constructor can be selected — true only when the mock is emitted into the
	/// model's own assembly, where the mirrored internal constructor is reachable.
	/// </param>
	private static IMethodSymbol? PickConstructor(IReadOnlyCollection<IMethodSymbol> constructors, bool includeInternal = false)
		=> constructors
			.Where(c => !c.IsStatic && IsCandidate(c, includeInternal) && ReadReactiveBindable(c) is not false)
			.OrderBy(c => c.Parameters.Length)
			.ThenBy(ParameterTypes, StringComparer.Ordinal)
			.FirstOrDefault();

	private static bool IsCandidate(IMethodSymbol ctor, bool includeInternal)
		=> ctor.DeclaredAccessibility == Accessibility.Public
			|| (includeInternal && ctor.DeclaredAccessibility == Accessibility.Internal);

	private static string? Generate(GeneratorExecutionContext context, ModelMock described)
	{
		var model = described.Model;

		// Command mocking is deferred to vNext: the consumer generator emits no command overrides for
		// now (the MVUX __Mock_SetCommand seam stays available for that future work).

		if (described.Inputs.Count == 0 && described.Derived.Count == 0)
		{
			// The original defect this generator had was producing nothing without saying so. A model with
			// feeds but no mockable one is a misclassification often enough to be worth reporting; Info so a
			// legitimately all-independent model cannot fail a warnings-as-errors build.
			context.ReportDiagnostic(Diagnostic.Create(
				NoMockableInput,
				model.Locations.FirstOrDefault(location => location.IsInSource) ?? Location.None,
				model.Name));
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

		// SetMock body.
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
		var accessibility = described.Accessibility;
		return $$"""
			// <auto-generated />
			#nullable enable
			{{nsHeader}}{{accessibility}} sealed record {{mockName}}
			{
			{{recordMembers.ToString().TrimEnd()}}
			}

			{{accessibility}} static partial class {{vmMockName}}
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

	/// <summary>
	/// Resolves the feed interface a member is mocked through. It is the interface, not the member's own
	/// type, that the mock exposes: a state is an <c>IFeed</c>, but <c>FeedMock.Empty</c> hands back an
	/// <c>IFeed</c>, so declaring the member as <c>IState</c> would not accept it (CS0266).
	/// </summary>
	private static bool TryGetFeed(ITypeSymbol type, out bool isList, out INamedTypeSymbol? feedInterface)
	{
		isList = false;
		feedInterface = null;

		var candidates = type.AllInterfaces.Concat(type is INamedTypeSymbol named ? new[] { named } : Array.Empty<INamedTypeSymbol>()).ToArray();

		foreach (var intf in candidates)
		{
			if (intf.OriginalDefinition.MetadataName == "IListFeed`1" && intf.TypeArguments.Length == 1)
			{
				isList = true;
				feedInterface = intf;
				return true;
			}
		}

		foreach (var intf in candidates)
		{
			if (intf.OriginalDefinition.MetadataName == "IFeed`1" && intf.TypeArguments.Length == 1)
			{
				feedInterface = intf;
				return true;
			}
		}

		return false;
	}

	private static string ParameterTypes(IMethodSymbol ctor)
		=> string.Join(",", ctor.Parameters.Select(p => FullName(p.Type)));

	private static string FullName(ITypeSymbol type)
		=> type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}
