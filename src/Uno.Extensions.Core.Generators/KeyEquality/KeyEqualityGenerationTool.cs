using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Equality;

namespace Uno.Extensions.Generators.KeyEquality;

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

	public KeyEqualityGenerationTool(KeyEqualityGenerationContext context)
	{
		_ctx = context;
		_assembly = context.Context.Compilation.Assembly;
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
		var assemblyImplicit = _assembly.FindAttribute<ImplicitKeysAttribute>() ?? new ImplicitKeysAttribute();
		var assemblyTypes = from module in _assembly.Modules from type in module.GetNamespaceTypes() select type;
		foreach (var type in assemblyTypes)
		{
			GetOrCreateConfig(type, assemblyImplicit);
		}
	}

	private Config? GetOrCreateConfig(INamedTypeSymbol? type, ImplicitKeysAttribute assemblyImplicit)
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
			var nonRecordIsIKeyEquatable = type.IsOrImplements(_ctx.IKeyEquatable, allowBaseTypes: true, out var nonRecordIKeyEquatable);
			var nonRecordIsIKeyed = type.IsOrImplements(_ctx.IKeyed, allowBaseTypes: true, out var nonRecordIKeyed);

			if (nonRecordIsIKeyEquatable || nonRecordIsIKeyed)
			{
				return _configs[type] = new Config(type, nonRecordIKeyEquatable, null, nonRecordIKeyed, null, new(0), NeedsCodeGen: false, IsCustomImplementation: true);
			}
			else
			{
				return null;
			}
		}

		var iKeyEquatable = _ctx.IKeyEquatable.Construct(type);
		var iKeyEquatable_GetKeyHashCode = iKeyEquatable.GetMethod(GetKeyHashCode);
		var iKeyEquatable_KeyEquals = iKeyEquatable.GetMethod(KeyEquals);
		var (baseIKeyEquatable, baseIKeyed) = type.BaseType is { } baseType ? GetBaseImplementations(baseType) : default;

		var hasIKeyEquatableDeclared = type.IsOrImplements(iKeyEquatable, allowBaseTypes: false, out _);
		var hasIKeyedDeclared = type.IsOrImplements(_ctx.IKeyed, allowBaseTypes: false, out var declaredIKeyed);
		var getKeyHashCode = type.FindLocalImplementationOf(iKeyEquatable_GetKeyHashCode, SymbolEqualityComparer.Default);
		var keyEquals = type.FindLocalImplementationOf(iKeyEquatable_KeyEquals, SymbolEqualityComparer.Default);
		var isCustomImplementation = getKeyHashCode is not null || keyEquals is not null;

		var keys = isCustomImplementation ? new (0) : SearchKeys(type, assemblyImplicit);
		var iKeyed = isCustomImplementation ? declaredIKeyed : _ctx.GetLocalIKeyed(baseIKeyed, keys);

		var isIKeyEquatable = keys is { Count: > 0 } || hasIKeyEquatableDeclared || getKeyHashCode is not null || keyEquals is not null;
		var needsCodeGen = isIKeyEquatable && (!hasIKeyEquatableDeclared || getKeyHashCode is null || keyEquals is null);

		// No keys found, nothing to do
		if (!isIKeyEquatable)
		{
			// If the type is not IKeyEquatable but inherits from type that is, we still add it to the configs (with code gen disabled) for the registry generation.
			return _configs[type] = baseIKeyEquatable is not null
				? new Config(type, null, baseIKeyEquatable, null, baseIKeyed, new(0), NeedsCodeGen: false, IsCustomImplementation: false)
				: null;
		}

		// Type is not partial, we cannot generate
		if (needsCodeGen && !type.IsPartial())
		{
			_ctx.Context.ReportDiagnostic(Rules.KE0001.GetDiagnostic(type, keys));

			return _configs[type] = null; // Reduce number of errors by considering type as not IKeyEquatable
		}

		// Make sure that if user partially implemented IKeyEquatable, he fully implements it, and he also provide IKeyed implementation
		if (getKeyHashCode is not null && keyEquals is null)
		{
			_ctx.Context.ReportDiagnostic(Rules.KE0002.GetDiagnostic(type, getKeyHashCode));
		}
		else if (getKeyHashCode is null && keyEquals is not null)
		{
			_ctx.Context.ReportDiagnostic(Rules.KE0003.GetDiagnostic(type, keyEquals));
		}
		if (isCustomImplementation && !hasIKeyedDeclared)
		{
			_ctx.Context.ReportDiagnostic(Rules.KE0006.GetDiagnostic(type, getKeyHashCode ?? keyEquals!));
		}

		return _configs[type] = new Config(type, iKeyEquatable, baseIKeyEquatable, iKeyed, baseIKeyed, keys, needsCodeGen, isCustomImplementation);

		(INamedTypeSymbol? iKeyEquatable, INamedTypeSymbol? iKeyed) GetBaseImplementations(INamedTypeSymbol baseType)
		{
			if (SymbolEqualityComparer.Default.Equals(baseType.ContainingAssembly, _assembly))
			{
				return GetOrCreateConfig(baseType, assemblyImplicit) is { } config
					? (config.IKeyEquatable, config.IKeyed ?? config.BaseIKeyed)
					: default;
			}
			else
			{
				baseType.IsOrImplements(_ctx.IKeyEquatable, out var baseKeyEquatable);
				baseType.IsOrImplements(_ctx.IKeyEquatable, out var baseKeyed);

				return (baseKeyEquatable, baseKeyed);
			}
		}
	}

	private List<IPropertySymbol> SearchKeys(INamedTypeSymbol type, ImplicitKeysAttribute assemblyImplicit)
	{
		// Search for properties flagged with [Key] attribute (Using Uno's attribute or System.ComponentModel.DataAnnotations.KeyAttribute)
		var keys = type
			.GetProperties()
			.Where(prop => prop.FindAttribute(_ctx.KeyAttribute) is not null
				|| (_ctx.DataAnnotationsKeyAttribute is {} daKey && prop.FindAttribute(daKey) is not null))
			.ToList();

		// If none found, search for implicit key properties
		if (keys is { Count: 0 })
		{
			var typeImplicit = type.FindAttribute<ImplicitKeysAttribute>();
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
			attributes: null,
			bases: $"{NS.Equality}.IKeyEquatable<{type}>{(config.IKeyed is null ? "" : ", " + config.IKeyed.ToFullString())}", // i.e. config.IKeyEquatable
			code: $@"
				{(config.IsCustomImplementation
					? "// Skipping IKeyed.Key as user is providing a custom implementation of IKeyEquatable"
					: $@"/// <inheritdoc cref=""{{NS.Equality}}.IKeyed{{T}}"" />
					{this.GetCodeGenAttribute()}
					{config.IKeyed!.TypeArguments[0].ToFullString()} {config.IKeyed.ToFullString()}.Key
					{{
						get
						{{
							{config switch
							{
								{ BaseIKeyed: null, Keys.Count: 1 } => $"return {config.Keys[0].Name};",
								{ BaseIKeyed: null } => $"return ({config.GetKeyNames()});",
								_ => $@"
								var baseKey = (({config.BaseIKeyed.ToFullString()})this).Key;
								return ({_ctx.DeconstructTupleOrSingle(config.BaseIKeyed!.TypeArguments[0]).Length switch
								{
									1 => "baseKey",
									var baseKeys => Enumerable.Range(1, baseKeys).Select(i => $"baseKey.Item{i}").Align(9, ",")
								 }},
								{config.GetKeyNames()});".Align(8)
							}}
						}}
					}}".Align(4))}

				{(config.IsCustomImplementation
					? $"// Skipping {GetKeyHashCode} as user is providing a custom implementation of IKeyEquatable"
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

				{(config.IsCustomImplementation
					? $"// Skipping {KeyEquals} as user is providing a custom implementation of IKeyEquatable"
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
				[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
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
					[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
					[global::System.Runtime.CompilerServices.ModuleInitializerAttribute]
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
								$@"if (type == typeof({type.ToFullString()}))
								{{
									return new {NS.Equality}.KeyEqualityComparer<{type.ToFullString()}>();
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
	/// <param name="IKeyEquatable">The bounded local implementation of IKeyEquatable.</param>
	/// <param name="BaseIKeyEquatable">The bounded base implementation of IKeyEquatable if any.</param>
	/// <param name="IKeyed">The bounded local implementation of IKeyed if any.</param>
	/// <param name="BaseIKeyed">The bounded base implementation of IKeyed if any.</param>
	/// <param name="Keys">The keys to use to generation.</param>
	/// <param name="NeedsCodeGen">Indicates is a partial class is needed.</param>
	/// <param name="IsCustomImplementation">Indicates the user is providing it own implementation of IKeyEquatable.</param>
	private record Config(
		INamedTypeSymbol Type,
		INamedTypeSymbol? IKeyEquatable,
		INamedTypeSymbol? BaseIKeyEquatable,
		INamedTypeSymbol? IKeyed,
		INamedTypeSymbol? BaseIKeyed,
		List<IPropertySymbol> Keys,
		bool NeedsCodeGen,
		[property: MemberNotNullWhen(true, nameof(Config.IKeyed))] bool IsCustomImplementation)
	{
		public string GetKeyNames(string separator = ", ")
			=> Keys.Select(k => k.Name).JoinBy(separator);
	}
}
