using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Uno.Extensions.Navigation;

/// <summary>
/// Reflection helpers used to find route types by name.
/// </summary>
public static class AssemblyExtensions
{
	/// <summary>
	/// Full names of the assemblies that implicit route lookup skips. <see cref="SafeGetTypes(Assembly)"/> adds an
	/// assembly whose types cannot be read at all.
	/// </summary>
	/// <remarks>
	/// Navigation locks on this list while it reads or adds to it. Code that changes it while navigation may be indexing
	/// types should lock on it too.
	/// </remarks>
	public static IList<string> Excludes { get; } = new List<string>();

	/// <summary>
	/// Gets the types of <paramref name="assembly"/> without throwing.
	/// </summary>
	/// <param name="assembly">The assembly to read.</param>
	/// <returns>
	/// The types that could be loaded. If the types cannot be read at all, the assembly is added to
	/// <see cref="Excludes"/> and the result is empty.
	/// </returns>
	[RequiresUnreferencedCode("From Assembly.GetTypes(): Types might be removed")]
	public static Type[] SafeGetTypes(this Assembly assembly)
		=> assembly.SafeGetTypes(out _);

	[RequiresUnreferencedCode("From Assembly.GetTypes(): Types might be removed")]
	internal static Type[] SafeGetTypes(this Assembly assembly, out Exception? error)
	{
		error = null;
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			// One type with a missing dependency must not hide the rest of the assembly.
			error = ex;
			return ex.Types.OfType<Type>().ToArray();
		}
		catch (Exception ex)
		{
			error = ex;
			if (assembly.FullName is not null)
			{
				lock (Excludes)
				{
					Excludes.Add(assembly.FullName);
				}
			}
			return new Type[] { };
		}
	}

	internal static bool IsExcluded(Assembly assembly)
	{
		lock (Excludes)
		{
			return Excludes.Contains(assembly.FullName ?? string.Empty);
		}
	}
}
