using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Equality;

namespace Uno.Extensions.Reactive.Generator.KeyEquality;

internal class KeyEqualityGenerationTool : ICodeGenTool
{
	private const string GetKeyHashCode = nameof(GetKeyHashCode);
	private const string KeyEquals = nameof(KeyEquals);

	private readonly KeyEqualityGenerationContext _ctx;
	private readonly IAssemblySymbol _assembly;

#pragma warning disable RS1024 // Compare symbols correctly // False positive
	private Dictionary<INamedTypeSymbol, Config?> _configs = new(SymbolEqualityComparer.Default);
#pragma warning restore RS1024 // Compare symbols correctly

	/// <inheritdoc />
	public string Version => "1";

	public KeyEqualityGenerationTool(KeyEqualityGenerationContext context, IAssemblySymbol assembly)
	{
		_ctx = context;
		_assembly = assembly;
	}

	public IEnumerable<(string fileName, string code)> Generate()
	{
		LoadConfigs();

		foreach (var kvp in _configs)
		{
			var type = kvp.Key;
			var config = kvp.Value;

			if (config is {NeedsCodeGen: true})
			{
				yield return (type.ToString(), GenerateKeyEquatable(type, config));
			}
		}

		if (GenerateProvider() is { } provider)
		{
			yield return ($"{_assembly.Name}.__KeyEqualityProvider", provider);
		}
	}

	private void LoadConfigs()
	{
		var assemblyImplicit = _assembly.FindAttribute<ImplicitKeyEqualityAttribute>() ?? new ImplicitKeyEqualityAttribute();
		var assemblyTypes = from module in _assembly.Modules from type in module.GetNamespaceTypes() select type;
		foreach (var type in assemblyTypes)
		{
			GetOrCreateConfig(type, assemblyImplicit);
		}
	}

	private Config? GetOrCreateConfig(INamedTypeSymbol? type, ImplicitKeyEqualityAttribute assemblyImplicit)
	{
		if (type is null)
		{
			return null;
		}

		if (_configs.TryGetValue(type, out var config))
		{
			return config;
		}

		if (!type.IsRecord)
		{
			// If the type is a class that implement IKeyEquatable, we still add it to the configs (with code gen disabled) for the registry generation.
			return _configs[type] = type.IsOrImplements(_ctx.IKeyEquatable, allowBaseTypes: true, out var implementation)
				? new Config(type, implementation, null, new (0), NeedsCodeGen: false, HasGetKeyHashCode: true, HasKeyEquals: true)
				: null;
		}

		var iKeyEquatable = _ctx.IKeyEquatable.Construct(type);
		var iKeyEquatable_GetKeyHashCode = iKeyEquatable.GetMethod(GetKeyHashCode);
		var iKeyEquatable_KeyEquals = iKeyEquatable.GetMethod(KeyEquals);
		var baseIKeyEquatable = type.BaseType is { } baseType ? GetBaseIKeyEquatable(baseType) : null;

		var hasIKeyEquatableDeclared = type.IsOrImplements(iKeyEquatable, allowBaseTypes: false, out _);
		var getKeyHashCode = type.FindLocalImplementationOf(iKeyEquatable_GetKeyHashCode, SymbolEqualityComparer.Default);
		var keyEquals = type.FindLocalImplementationOf(iKeyEquatable_KeyEquals, SymbolEqualityComparer.Default);
		var keys = SearchKeys(type, assemblyImplicit);

		var isIKeyEquatable = keys is { Count: > 0 } || hasIKeyEquatableDeclared || getKeyHashCode is not null || keyEquals is not null;
		var needsCodeGen = isIKeyEquatable && (!hasIKeyEquatableDeclared || getKeyHashCode is null || keyEquals is null);

		// No keys found, nothing to do
		if (!isIKeyEquatable)
		{
			// If the type is not IKeyEquatable but inherits from type that is, we still add it to the configs (with code gen disabled) for the registry generation.
			return _configs[type] = baseIKeyEquatable is not null
				? new Config(type, null, baseIKeyEquatable, new(0), NeedsCodeGen: false, HasGetKeyHashCode: false, HasKeyEquals: false)
				: null;
		}

		// Type is not partial, we cannot generate
		if (needsCodeGen && !type.IsPartial())
		{
			_ctx.Context.ReportDiagnostic(Rules.KE0001.GetDiagnostic(type, keys));

			return _configs[type] = null; // Reduce number of errors by considering type as not IKeyEquatable
		}

		if (getKeyHashCode is not null && keyEquals is null)
		{
			_ctx.Context.ReportDiagnostic(Rules.KE0002.GetDiagnostic(type, getKeyHashCode));
		}
		else if (getKeyHashCode is null && keyEquals is not null)
		{
			_ctx.Context.ReportDiagnostic(Rules.KE0003.GetDiagnostic(type, keyEquals));
		}

		return _configs[type] = new Config(type, iKeyEquatable, baseIKeyEquatable, keys, needsCodeGen, getKeyHashCode is not null, keyEquals is not null);

		INamedTypeSymbol? GetBaseIKeyEquatable(INamedTypeSymbol baseType)
			=> SymbolEqualityComparer.Default.Equals(baseType.ContainingAssembly, _assembly)
				? GetOrCreateConfig(baseType, assemblyImplicit)?.IKeyEquatable
				: (baseType.IsOrImplements(_ctx.IKeyEquatable, out var baseKeyEquatable) ? baseKeyEquatable : null);
	}

	private List<IPropertySymbol> SearchKeys(INamedTypeSymbol type, ImplicitKeyEqualityAttribute assemblyImplicit)
	{
		// Search for properties flagged with [Key] attribute
		var keys = type
			.GetProperties()
			.Where(prop => prop.FindAttribute(_ctx.KeyAttribute) is not null)
			.ToList();

		// If none found, search for implicit key properties
		if (keys is { Count: 0 })
		{
			var typeImplicit = type.FindAttribute<ImplicitKeyEqualityAttribute>();
			var @implicit = typeImplicit ?? assemblyImplicit;
			if (@implicit.IsEnabled)
			{
				keys = @implicit
					.PropertyNames
					.Select(implicitKeyName => type.FindProperty(implicitKeyName, allowBaseTypes: false, comparison: StringComparison.OrdinalIgnoreCase))
					.Where(implicitKeyProp => implicitKeyProp is not null)
					.ToList();

				if (keys is { Count: 0 } && typeImplicit is not null)
				{
					// Type flagged with implicit attribute, but no matching keys
					_ctx.Context.ReportDiagnostic(Rules.KE0004.GetDiagnostic(type, typeImplicit.PropertyNames));
				}
				else if (keys is { Count: > 1 })
				{
					// The type has more than one matching implicit key, we used only the first and we raise warning
					var usedKey = keys.First();
					_ctx.Context.ReportDiagnostic(Rules.KE0005.GetDiagnostic(type, keys, usedKey));

					keys = new List<IPropertySymbol> { usedKey };
				}
			}
		}

		return keys;
	}

	private string GenerateKeyEquatable(INamedTypeSymbol type, Config config)
	{
		var modifiers = config switch
		{
			{ BaseIKeyEquatable: not null } when type.IsSealed => "override sealed",
			{ BaseIKeyEquatable: not null } => "override",
			_ when type.IsSealed => "",
			_ => "virtual",
		};

		return this.AsPartialOf(
			type,
			$"{NS.Equality}.IKeyEquatable<{type}>", // i.e. config.IKeyEquatable
			$@"
				{(config.HasGetKeyHashCode
					? $"// Skipping {GetKeyHashCode} as it has already been implemented in user's code"
					: $@"/// <inheritdoc cref=""{NS.Equality}.IKeyEquatable{{T}}"" />
					{this.GetCodeGenAttribute()}
					public {modifiers} int {GetKeyHashCode}()
					{{
						unchecked
						{{
							var hash = {config switch
							{
								{ BaseIKeyEquatable: { } @base } when type.BaseType!.FindMethod(GetKeyHashCode) is {MethodKind: MethodKind.ExplicitInterfaceImplementation} => $@"(({@base})this).GetKeyHashCode()",
								{ BaseIKeyEquatable: not null } => @"base.GetKeyHashCode()",
								{ Type.IsReferenceType: true } => "global::System.Collections.Generic.EqualityComparer<global::System.Type>.Default.GetHashCode(EqualityContract) * -1521134295",
								_ => "0",
							}};

							{config.Keys.Select(key => $@"
							hash += global::System.Collections.Generic.EqualityComparer<{key.Type}>.Default.GetHashCode({key.Name});
							hash *= -1521134295;").Align(7)}

							return hash;
						}}
					}}".Align(4))}

				{(config.HasKeyEquals
					? $"// Skipping {KeyEquals} as it has already been implemented in user's code"
					: $@"/// <inheritdoc cref=""{NS.Equality}.IKeyEquatable{{T}}"" />
					{this.GetCodeGenAttribute()}
					public bool {KeyEquals}({type}{(type.IsValueType ? "" : "?")} other)
					{{
						{config switch
						{
							{ BaseIKeyEquatable: { } @base } when type.BaseType!.FindMethod(KeyEquals) is {MethodKind: MethodKind.ExplicitInterfaceImplementation} =>
								$@"if (!(({@base})this).KeyEquals(other))
								{{
									return false;
								}}".Align(6),
							{ BaseIKeyEquatable: not null } =>
								$@"if (!base.KeyEquals(other))
								{{
									return false;
								}}".Align(6),
							{ Type.IsReferenceType: true } =>
								@"if (object.ReferenceEquals(this, other))
								{
									return true;
								}

								if (object.ReferenceEquals(null, other)
									|| EqualityContract != other.EqualityContract)
								{
									return false;
								}".Align(6),
							_ => "",
						}}

						return {config.Keys.Select(key => $"global::System.Collections.Generic.EqualityComparer<{key.Type}>.Default.Equals({key.Name}, other.{key.Name})").Align(7, "&& ")};
					}}".Align(4))}");
	}

	private string? GenerateProvider()
	{
		var types = _configs.Values
			.Where(config => config is not null)
			.Select(config => config!.Type)
			.ToImmutableList();

		if (types is { Count: 0 })
		{
			return null;
		}

		return $@"{this.GetFileHeader(3)}

			using global::System;
			using global::System.Linq;
			using global::System.Threading.Tasks;

			namespace {_assembly.Name}
			{{
				/// <summary>
				/// An <see cref=""{NS.Equality}.IKeyEqualityProvider""/> which allows resolution of a <see cref=""{NS.Equality}.KeyEqualityComparer{{T}}""/>
				/// for all <see cref=""{NS.Equality}.IKeyEquatable{{T}}""/> declared in the Uno.Extensions.Core assembly.
				/// </summary>
				/// <remarks>This provider is automatically registered into the <see cref=""{NS.Equality}.KeyEqualityComparer""/> on module load.</remarks>
				[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
				{this.GetCodeGenAttribute()}
				internal sealed class __KeyEqualityProvider : {NS.Equality}.IKeyEqualityProvider
				{{
					/// <summary>
					/// Register this provider into the <see cref=""{NS.Equality}.KeyEqualityComparer""/> registry.
					/// </summary>
					/// <remarks>
					/// This method is flagged with the [ModuleInitializerAttribute] which means that it will be invoked by the runtime when the module is being loaded.
					/// You should not have to use it at any time.
					/// </remarks>
					[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
					[System.Runtime.CompilerServices.ModuleInitializerAttribute]
					internal static void Initialize()
						=> {NS.Equality}.KeyEqualityComparer.Register(new __KeyEqualityProvider());

					// Should not be used externally, only by the Initialize method.
					private __KeyEqualityProvider()
					{{
					}}

					/// <inheritdoc />
					global::System.Collections.IEqualityComparer? {NS.Equality}.IKeyEqualityProvider.TryGet(Type type)
					{{
						{types
							.Select(type =>
								$@"if (type == typeof({type}))
								{{
									return new {NS.Equality}.KeyEqualityComparer<{type}>();
								}}")
							.Align(6)}

						return null;
					}}
				}}
			}}
			".Align(0);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="Type"></param>
	/// <param name="IKeyEquatable">The bounded implementation of IKeyEquatable.</param>
	/// <param name="BaseIKeyEquatable">The bounded base implementation of IKeyEquatable if any.</param>
	/// <param name="Keys">The keys to use to generation.</param>
	/// <param name="NeedsCodeGen">Indicates is a partial class is needed.</param>
	/// <param name="HasGetKeyHashCode">Indicates if the GetKeyHashCode has already been implemented.</param>
	/// <param name="HasKeyEquals">Indicates if the KeyEquals has already been implemented.</param>
	private record Config(
		INamedTypeSymbol Type,
		INamedTypeSymbol? IKeyEquatable,
		INamedTypeSymbol? BaseIKeyEquatable,
		List<IPropertySymbol> Keys,
		bool NeedsCodeGen,
		bool HasGetKeyHashCode,
		bool HasKeyEquals);
}
