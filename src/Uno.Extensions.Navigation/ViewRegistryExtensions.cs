namespace Uno.Extensions.Navigation;

public static class ViewRegistryExtensions
{
	public static ViewMap FindByViewModel<TViewModel>(this IViewRegistry registry)
	{
		return registry.FindByViewModel(typeof(TViewModel));
	}

	public static ViewMap FindByViewModel(this IViewRegistry registry, Type? viewModelType)
	{
		return registry.Items.FindByInheritedTypes(viewModelType, map => map.ViewModel).FirstOrDefault()
			?? throw new InvalidOperationException($"A ViewMap for type {viewModelType} could not be found");
	}

	public static ViewMap FindByView<TView>(this IViewRegistry registry)
	{
		return registry.FindByView(typeof(TView));
	}

	public static ViewMap FindByView(this IViewRegistry registry, Type? viewType)
	{
		return registry.Items.FindByInheritedTypes(viewType, map => map.View).FirstOrDefault()
			?? throw new InvalidOperationException($"A ViewMap for type {viewType} could not be found");
	}

	public static ViewMap FindByData<TData>(this IViewRegistry registry)
	{
		return registry.FindByData(typeof(TData));
	}
	public static ViewMap FindByData(this IViewRegistry registry, Type? dataType)
	{
		return registry.Items.FindByInheritedTypes(dataType, map => map.Data?.Data).FirstOrDefault()
			?? throw new InvalidOperationException($"A ViewMap for type {dataType} could not be found");
	}

	public static ViewMap FindByResultData<TResultData>(this IViewRegistry registry)
	{
		return registry.FindByResultData(typeof(TResultData));
	}
	public static ViewMap FindByResultData(this IViewRegistry registry, Type? dataType)
	{
		return registry.Items.FindByInheritedTypes(dataType, map => map.ResultData).FirstOrDefault()
			?? throw new InvalidOperationException($"A ViewMap for type {dataType} could not be found");
	}
}

