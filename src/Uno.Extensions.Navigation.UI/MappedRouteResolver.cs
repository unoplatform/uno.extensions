﻿namespace Uno.Extensions.Navigation;

public class MappedRouteResolver : RouteResolverDefault
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

	protected override RouteInfo? InternalDefaultMapping(string? path = null, Type? view = null, Type? viewModel = null)
	{
		// Check to see if the viewmodel type specified is actually a mapped viewmodel (eg a bindableviewmodel in case of mvux)
		// If it is, set viewModel to be the un-mapped viewmodel type so that the routemap can be correctly created.
		if(viewModel is not null &&
			_viewModelMappings.FirstOrDefault(x=>x.Value==viewModel) is { } mapping)
		{
			viewModel = mapping.Key;
		}

		var routeInfo = base.InternalDefaultMapping(path, view, viewModel);
		if (routeInfo?.ViewModel != null &&
			_viewModelMappings.TryGetValue(routeInfo.ViewModel, out var bindableViewModel))
		{
			return FromRouteMap(new RouteMap(
				Path: routeInfo.Path,
				View: new MappedViewMap(
						View: null,
						ViewSelector: routeInfo.View,
						ViewModel: routeInfo.ViewModel,
						Data: new DataMap(
							Data: routeInfo.Data,
							UntypedToQuery: routeInfo.ToQuery,
							UntypedFromQuery: routeInfo.FromQuery),
						ResultData: routeInfo.ResultData,
						MappedViewModel: bindableViewModel),
				IsDefault: routeInfo.IsDefault,
				DependsOn: routeInfo.DependsOn,
				Init: routeInfo.Init
				));
		}
		return routeInfo;
	}
}
