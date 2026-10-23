using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;

namespace Uno.Extensions.Navigation;

/// <summary>
/// Builds the by-name type index that <see cref="RouteResolverDefault"/> searches for implicit routes.
/// </summary>
/// <remarks>
/// Kept free of UI types so the unit tests can compile it in directly; Uno.Extensions.Navigation.WinUI itself cannot be
/// loaded in a plain test host.
/// </remarks>
internal static class RouteTypeIndex
{
	/// <summary>
	/// The load context that loaded <paramref name="anchor"/>, or the default context for an assembly that has none.
	/// </summary>
	internal static AssemblyLoadContext OwnContextOf(Assembly anchor)
		=> AssemblyLoadContext.GetLoadContext(anchor) ?? AssemblyLoadContext.Default;

	/// <summary>
	/// The assemblies of <paramref name="ownContext"/>, followed by those of the default context.
	/// </summary>
	/// <remarks>
	/// The own context comes first, and <see cref="Build"/> keeps the first type per name, so the own context wins over a
	/// same-named type in the default one. A default-context assembly whose simple name the own context also has is
	/// skipped: code in the own context binds to its own copy, never to the default one.
	/// </remarks>
	internal static Assembly[] OwnAndDefaultContextAssemblies(AssemblyLoadContext ownContext)
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
	internal static IDictionary<string, Type> Build(IEnumerable<Assembly> assemblies)
		=> (from asb in assemblies
			where (!(asb.FullName ?? string.Empty).StartsWith("_") && !AssemblyExtensions.Excludes.Contains(asb.FullName ?? string.Empty))
			from t in asb.SafeGetTypes()
			where t.IsClass
			select new { t.Name, Type = t }).ToDictionaryDistinct(x => x.Name, x => x.Type);
}
