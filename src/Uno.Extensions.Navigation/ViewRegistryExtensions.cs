namespace Uno.Extensions.Navigation;

public static class ViewRegistryExtensions
{
	public static ViewMap FindByViewModel<TViewModel>(this IViewRegistry registry)
	{
		return registry.FindByViewModel(typeof(TViewModel));
	}

	public static ViewMap FindByViewModel(this IViewRegistry registry, Type? viewModelType)
	{
		viewModelType = viewModelType ?? throw new ArgumentNullException(nameof(viewModelType));
		var viewMap = registry.Items.FindByInheritedTypes(viewModelType, map => map.ViewModel).FirstOrDefault();
		if (viewMap is null)
			throw new ViewMapNotFoundByViewModelException(viewModelType);
		return viewMap;
	}

	public static ViewMap FindByView<TView>(this IViewRegistry registry)
	{
		return registry.FindByView(typeof(TView));
	}

	public static ViewMap FindByView(this IViewRegistry registry, Type? viewType)
	{
		viewType = viewType ?? throw new ArgumentNullException(nameof(viewType));
		var viewMap = registry.Items.FindByInheritedTypes(viewType, map => map.View).FirstOrDefault();
		if (viewMap is null)
			throw new ViewMapNotFoundByViewException(viewType);
		return viewMap;
	}

	public static ViewMap FindByData<TData>(this IViewRegistry registry)
	{
		return registry.FindByData(typeof(TData));
	}
	public static ViewMap FindByData(this IViewRegistry registry, Type? dataType)
	{
		dataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
		var viewMap = registry.Items.FindByInheritedTypes(dataType, map => map.Data?.Data).FirstOrDefault();
		if (viewMap is null)
			throw new ViewMapNotFoundByDataException(dataType);
		return viewMap;
	}

	public static ViewMap FindByResultData<TResultData>(this IViewRegistry registry)
	{
		return registry.FindByResultData(typeof(TResultData));
	}
	public static ViewMap FindByResultData(this IViewRegistry registry, Type? dataType)
	{
		dataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
		var viewMap = registry.Items.FindByInheritedTypes(dataType, map => map.ResultData).FirstOrDefault();
		if (viewMap is null)
			throw new ViewMapNotFoundByResultDataException(dataType);
		return viewMap;
	}
}

