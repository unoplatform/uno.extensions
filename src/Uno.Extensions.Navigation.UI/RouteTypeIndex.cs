using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;

namespace Uno.Extensions.Navigation;

/// <summary>
/// Builds the by-name type index that <see cref="RouteResolverDefault"/> searches for implicit routes.
/// </summary>
/// <remarks>
/// Must stay UI-free: Uno.Extensions.Navigation.Tests compiles this file in directly.
/// </remarks>
internal static class RouteTypeIndex
{
	/// <summary>
	/// Indexes the classes of the load context that loaded <paramref name="anchor"/>, then those of the default context.
	/// </summary>
	/// <param name="anchor">The assembly whose load context is searched first.</param>
	/// <returns>The load context searched first, and the index.</returns>
	[RequiresUnreferencedCode("From Assembly.GetTypes(): Types might be removed")]
	internal static (AssemblyLoadContext OwnContext, IDictionary<string, Type> Types) For(Assembly anchor)
	{
		var ownContext = AssemblyLoadContext.GetLoadContext(anchor) ?? AssemblyLoadContext.Default;
		return (ownContext, Build(OwnAndDefaultContextAssemblies(ownContext)));
	}

	/// <summary>
	/// The assemblies of <paramref name="ownContext"/>, followed by those of the default context.
	/// </summary>
	/// <remarks>
	/// The own context comes first, and <see cref="Build"/> keeps the first type per name, so the own context wins over a
	/// same-named type in the default one. A default-context assembly whose simple name the own context also has is
	/// skipped: code in the own context binds to its own copy, never to the default one.
	/// </remarks>
	private static Assembly[] OwnAndDefaultContextAssemblies(AssemblyLoadContext ownContext)
	{
		var ownAssemblies = ownContext.Assemblies.ToArray();
		if (ownContext == AssemblyLoadContext.Default)
		{
			return ownAssemblies;
		}

		var ownNames = new HashSet<string?>(ownAssemblies.Select(asb => asb.GetName().Name), StringComparer.OrdinalIgnoreCase);
		return ownAssemblies
			.Concat(AssemblyLoadContext.Default.Assemblies.Where(asb => !ownNames.Contains(asb.GetName().Name)))
			.ToArray();
	}

	/// <summary>
	/// Maps each class name in <paramref name="assemblies"/> to its type, keeping the first type found for a name.
	/// </summary>
	[RequiresUnreferencedCode("From Assembly.GetTypes(): Types might be removed")]
	private static IDictionary<string, Type> Build(IEnumerable<Assembly> assemblies)
	{
		var types = new Dictionary<string, Type>();
		foreach (var asb in assemblies)
		{
			if ((asb.FullName ?? string.Empty).StartsWith("_") || AssemblyExtensions.Excludes.Contains(asb.FullName ?? string.Empty))
			{
				continue;
			}

			foreach (var t in asb.SafeGetTypes())
			{
				if (t.IsClass)
				{
					types.TryAdd(t.Name, t);
				}
			}
		}

		return types;
	}
}
