using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Generators;

internal static class GenerationContext
{
	public static bool IsDisabled(this GeneratorExecutionContext context, string disableGeneratorPropertyName, bool defaultValue = false)
		=> bool.TryParse(context.GetMSBuildPropertyValue(disableGeneratorPropertyName, defaultValue.ToString()), out var isDisabled) && isDisabled;

	public static TContext? TryGet<TContext>(GeneratorExecutionContext context, [NotNullWhen(true)] out string? error)
		where TContext : notnull
	{
		try
		{
			var compilation = context.Compilation;
			var ctor = typeof(TContext)
				.GetConstructors()
				.Single(ctor => (ctor.GetParameters() is { Length: > 1 } parameters && parameters.Skip(1).All(p => p.ParameterType == typeof(INamedTypeSymbol))));

			var arguments = ctor
				.GetParameters()
				.Skip(1) // GeneratorExecutionContext
				.Select(parameter => (parameter, attribute: parameter.GetCustomAttribute<ContextTypeAttribute>()))
				.Where(x => x.attribute is not null)
				.Select(x =>
				(
					x.parameter,
					type: x.attribute.Type,
					isOptional: x.attribute.IsOptional || x.parameter.GetCustomAttributesData().Any(attr => attr.AttributeType.FullName.Equals("System.Runtime.CompilerServices.NullableAttribute")),
					symbol: GetTypesByMetadataName(compilation, x.attribute.Type)
						.OrderBy(t => t switch
							{
								_ when SymbolEqualityComparer.Default.Equals(t.ContainingAssembly, compilation.Assembly) => 0,
								_ when t.IsAccessibleTo(compilation.Assembly) => 1,
								_ => 2
							})
						.FirstOrDefault()
				))
				.ToList();

			if (arguments
				.Where(arg => arg is {symbol: null, isOptional: false})
				.ToList() is { Count: > 0 } missingArgs)
			{
				error = $"Failed to resolve types {missingArgs.Select(arg => arg.type).JoinBy(", ")}";
				return default;
			}

			error = null;
			return (TContext)ctor.Invoke(new object[] { context }.Concat(arguments.Select(arg => arg.symbol)).ToArray());
		}
		catch (Exception ex)
		{
			error = "Failed to initialize key equality generation context.\r\n" + ex;
			return default;
		}
	}


	#region GetTypesByMetadataName
	// Starting from Roslyn 4.2 there is public method GetTypesByMetadataName which returns all possible types
	// (while GetTypeByMetadataName -without a s- returns a type only there **ONE** matching type)
	// But as of 2022/11/02 linux and macOS agents are still running with roslyn 4.1.x,
	// so using that method directly would cause generators to fail, unless users explicitly add a ref to Microsoft.Net.Compilers.Toolset 4.2.0
	// (Note: that ref cannot be embedded in our packages)
	// Note: if roslyn is recent enough, we try to rely on that method using reflection in order to take advantage of the internal caching of roslyn.
	private static readonly Func<Compilation, string, ImmutableArray<INamedTypeSymbol>> GetTypesByMetadataName = typeof(Compilation)
			.GetMethods(BindingFlags.Instance | BindingFlags.Public)
			.FirstOrDefault(method => method is { Name: nameof(GetTypesByMetadataName) }
				&& method.GetParameters() is { Length: 1 } parameters
				&& parameters[0].ParameterType == typeof(string)
				&& method.ReturnType == typeof(ImmutableArray<INamedTypeSymbol>))
		is { } roslynMethod
		? (compilation, fullyQualifiedMetadataName) => (ImmutableArray<INamedTypeSymbol>)roslynMethod.Invoke(compilation, new[] { fullyQualifiedMetadataName })
		: GetTypesByMetadataName_LocalImpl;

	private static readonly ConditionalWeakTable<string, ImmutableList<INamedTypeSymbol>> _getTypesCache = new();

	/// <summary>
	/// Gets all types with the compilation's assembly and all referenced assemblies that have the
	/// given canonical CLR metadata name. Accessibility to the current assembly is ignored when
	/// searching for matching type names.
	/// </summary>
	/// <returns>Empty array if no types match. Otherwise, all types that match the name, current assembly first if present.</returns>
	/// <remarks>
	/// <para>
	/// Assemblies can contain multiple modules. Within each assembly, the search is performed based on module's position in the module list of that assembly. When
	/// a match is found in one module in an assembly, no further modules within that assembly are searched.
	/// </para>
	/// <para>Type forwarders are ignored, and not considered part of the assembly where the TypeForwardAttribute is written.</para>
	/// </remarks>
	private static ImmutableArray<INamedTypeSymbol> GetTypesByMetadataName_LocalImpl(Compilation compilation, string fullyQualifiedMetadataName)
	{
		// This imported from / inspired by https://github.com/dotnet/roslyn/blob/afddda5f6775800a32706f7055955042b5cfce7a/src/Compilers/Core/Portable/Compilation/Compilation.cs#L1208

		ImmutableList<INamedTypeSymbol> val;
		lock (_getTypesCache)
		{
			if (!_getTypesCache.TryGetValue(fullyQualifiedMetadataName, out val))
			{
				val = getTypesByMetadataNameImpl();
				_getTypesCache.Add(fullyQualifiedMetadataName, val);
			}
		}

		return val.ToImmutableArray();

		ImmutableList<INamedTypeSymbol> getTypesByMetadataNameImpl()
		{
			List<INamedTypeSymbol>? typesByMetadataName = null;

			// Start with the current assembly, then corlib, then look through all references, to mimic GetTypeByMetadataName search order.

			addIfNotNull(compilation.Assembly.GetTypeByMetadataName(fullyQualifiedMetadataName));

			var corLib = compilation.ObjectType.ContainingAssembly;

			if (!ReferenceEquals(corLib, compilation.Assembly))
			{
				addIfNotNull(corLib.GetTypeByMetadataName(fullyQualifiedMetadataName));
			}

			foreach (var referencedAssembly in compilation.SourceModule.ReferencedAssemblySymbols)
			{
				if (ReferenceEquals(referencedAssembly, corLib))
				{
					continue;
				}

				addIfNotNull(referencedAssembly.GetTypeByMetadataName(fullyQualifiedMetadataName));
			}

			return typesByMetadataName?.ToImmutableList() ?? ImmutableList<INamedTypeSymbol>.Empty;

			void addIfNotNull(INamedTypeSymbol? toAdd)
			{
				if (toAdd != null)
				{
					typesByMetadataName ??= new List<INamedTypeSymbol>();
					typesByMetadataName.Add(toAdd);
				}
			}
		}
	} 
	#endregion
}
