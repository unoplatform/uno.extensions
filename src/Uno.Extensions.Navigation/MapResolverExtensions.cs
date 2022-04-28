

namespace Uno.Extensions.Navigation;

public static class MapResolverExtensions
{
	public static TMap? FindByInheritedTypes<TMap>(this IEnumerable<TMap> mappings, Type? typeToFind, Func<TMap, Type?> mapType)
	{
		if (typeToFind is null)
		{
			return default;
		}

		// Handle the non-reflection check first
		var map = (from m in mappings
				   where mapType(m) == typeToFind
				   select m)
				   .FirstOrDefault();
		if (map is not null)
		{
			return map;
		}

		return (from baseType in typeToFind.GetBaseTypes()
				from m in mappings
				where mapType(m) == baseType
				select m)
				   .FirstOrDefault();
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
