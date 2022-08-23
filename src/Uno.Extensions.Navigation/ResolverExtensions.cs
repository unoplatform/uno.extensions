

namespace Uno.Extensions.Navigation;

public static class ResolverExtensions
{
	public static TMap[] FindByInheritedTypes<TMap>(this IEnumerable<TMap> mappings, Type? typeToFind, Func<TMap, Type?> mapType)
	{
		if (typeToFind is null)
		{
			return Array.Empty<TMap>();
		}

		// Handle the non-reflection check first
		var map = (from m in mappings
				   where mapType(m) == typeToFind
				   select m)
				   .ToArray();
		if (map.Any())
		{
			return map.ToArray();
		}

		return (from baseType in typeToFind.GetBaseTypes()
				from m in mappings
				where mapType(m) == baseType
				select m)
				   .ToArray();
	}

	public static IEnumerable<Type> GetBaseTypes(this Type type)
	{
		var previousType = type;
		while (true)
		{
#if !WINDOWS_UWP
			var baseType = previousType.BaseType;
#else
			var baseType = previousType.GetTypeInfo().BaseType;
#endif
			if (baseType == null || baseType.FullName == previousType.FullName)
			{
				yield break;
			}
			else
			{
				yield return baseType;
				previousType = baseType;
			}
		}
	}
}
