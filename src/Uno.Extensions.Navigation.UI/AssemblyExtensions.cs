using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Uno.Extensions.Navigation;

public static class AssemblyExtensions
{
	public static IList<string> Excludes { get; } = new List<string>();

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
