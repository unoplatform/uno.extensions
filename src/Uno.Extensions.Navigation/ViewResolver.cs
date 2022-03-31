namespace Uno.Extensions.Navigation;

public class ViewResolver : IViewResolver
{
	private readonly ViewMap[] _views;

	public ViewResolver(IViewRegistry registry)
	{
		_views = registry.Items.ToArray();
	}

	public virtual ViewMap? FindByViewModel(Type? viewModelType)
	{
		return FindByInheritedTypes(_views, viewModelType, map => map.ViewModel);
	}

	public virtual ViewMap? FindByView(Type? viewType)
	{
		return FindByInheritedTypes(_views, viewType, map => map.RenderView);
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
