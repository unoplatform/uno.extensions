namespace Uno.Extensions.Navigation;

public class MappedRouteResolver : RouteResolver
{
	private readonly IDictionary<Type, Type> _viewModelMappings;
	public MappedRouteResolver(
		ILogger<MappedRouteResolver> logger,
		IRouteRegistry routes,
		MappedViewRegistry views) : base(logger, routes, views)
	{
		_viewModelMappings = views.ViewModelMappings;
	}

	protected override RouteInfo FromRouteMap(RouteMap drm)
	{
		var viewFunc = (drm.View?.View is not null) ?
										() => drm.View.View :
										drm.View?.ViewSelector;
		return AssignParentRouteInfo(new RouteInfo(
			Path: drm.Path,
			View: viewFunc,
			ViewAttributes: drm.View?.ViewAttributes,
			ViewModel: (drm.View is MappedViewMap rvmp) ? rvmp.MappedViewModel : drm.View?.ViewModel,
			Data: drm.View?.Data?.Data,
			ToQuery: drm.View?.Data?.UntypedToQuery,
			FromQuery: drm.View?.Data?.UntypedFromQuery,
			ResultData: drm.View?.ResultData,
			IsDefault: drm.IsDefault,
			DependsOn: drm.DependsOn,
			Init: drm.Init,
			IsDialogViewType: () =>
			{
				return IsDialogViewType(viewFunc?.Invoke());
			},
			Nested: ResolveViewMaps(drm.Nested)));
	}

	protected override RouteInfo[] InternalFindByViewModel(Type? viewModelType)
	{
		if (viewModelType is not null &&
			_viewModelMappings.TryGetValue(viewModelType, out var bindableViewModel))
		{
			return base.InternalFindByViewModel(bindableViewModel);
		}
		return base.InternalFindByViewModel(viewModelType);
	}
}
