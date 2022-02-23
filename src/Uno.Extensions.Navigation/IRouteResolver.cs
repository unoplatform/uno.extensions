using System.Reflection;

namespace Uno.Extensions.Navigation;

public interface IRouteResolver
{
	RouteMap? Find(Route route);

	RouteMap? FindByPath(string? path);

	RouteMap? FindByViewModel(Type? viewModelType);

	RouteMap? FindByView(Type? viewType);

	RouteMap? FindByData(Type? dataType);

	RouteMap? FindByResultData(Type? resultDataType);
}


public interface IViewResolver
{
	ViewMap? FindByViewModel(Type? viewModelType);

	ViewMap? FindByView(Type? viewType);

	ViewMap? FindByData(Type? dataType);

	ViewMap? FindByResultData(Type? resultDataType);
}


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


public class ViewResolver : IViewResolver
{
	private readonly ViewMap[] _views;

	public ViewResolver(IViewRegistry registry)
	{
		_views = registry.Views.ToArray();
	}

	public virtual ViewMap? FindByViewModel(Type? viewModelType)
	{
		return FindByInheritedTypes(_views, viewModelType, map => map.ViewModel);
	}

	public virtual ViewMap? FindByView(Type? viewType)
	{
		return FindByInheritedTypes(_views, viewType, map => map.View);
	}

	public ViewMap? FindByData(Type? dataType)
	{
		return FindByInheritedTypes(_views, dataType, map => map.Data?.Data);
	}

	public ViewMap? FindByResultData(Type? dataType)
	{
		return FindByInheritedTypes(_views, dataType, map => map.ResultData);
	}

	private TMap? FindByInheritedTypes<TMap>(IEnumerable<TMap> mappings, Type? typeToFind, Func<TMap, Type?> mapType)
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

	
}
