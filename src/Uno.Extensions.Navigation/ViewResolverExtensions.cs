using System.Reflection;

namespace Uno.Extensions.Navigation;

public static class ViewResolverExtensions
{
	public static ViewMap? FindByViewModel<TData>(this IViewResolver resolver)

	{
		return resolver.FindByViewModel(typeof(TData));
	}

	public static ViewMap? FindByView<TData>(this IViewResolver resolver)

	{
		return resolver.FindByView(typeof(TData));
	}

	public static ViewMap? FindByData<TData>(this IViewResolver resolver)

	{
		return resolver.FindByData(typeof(TData));
	}

	public static ViewMap? FindByResultData<TData>(this IViewResolver resolver)

	{
		return resolver.FindByResultData(typeof(TData));
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
