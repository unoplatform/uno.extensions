//-:cnd:noEmit
using System;
using System.Collections.Generic;
using MyExtensionsApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation;


namespace MyExtensionsApp.Reactive;

// Need an attribute to identify this as the static class where
// the ViewModelMappings should be source generated
// [ReactiveMappings]
public static class ReactiveViewModelMappings
{
	// ********* Generated ********* //
	public static IDictionary<Type, Type> ViewModelMappings = new Dictionary<Type, Type>()
			{
				{ typeof(LoginViewModel), typeof(LoginViewModel.BindableLoginViewModel)},
				{ typeof(ProductsViewModel),typeof(ProductsViewModel.BindableProductsViewModel)},
				{ typeof(ProductDetailsViewModel),typeof(ProductDetailsViewModel.BindableProductDetailsViewModel)},
				{ typeof(FiltersViewModel),typeof(FiltersViewModel.BindableFiltersViewModel)},
				{ typeof(CartProductDetailsViewModel),typeof(CartProductDetailsViewModel.BindableCartProductDetailsViewModel )}
			};
	// ***************************** //
}

// ********* Classes to be added to Reactive.Navigation ********* //
public class ReactiveViewRegistry : ViewRegistry
{
	private readonly IDictionary<Type, Type> _viewModelMappings;
	public ReactiveViewRegistry(IServiceCollection services, IDictionary<Type, Type> viewModelMappings) : base(services)
	{
		_viewModelMappings = viewModelMappings;
	}

	protected override void InsertItem(ViewMap item)
	{
		if (item.ViewModel is not null &&
			_viewModelMappings.TryGetValue(item.ViewModel, out var bindableViewModel))
		{
			item = item.WithBindableViewModel(bindableViewModel);
		}

		base.InsertItem(item);
	}
}

public static class ReactiveViewResolverExtension
{
	public static ViewMap WithBindableViewModel(this ViewMap item, Type bindableViewModel)
	{
		return new ReactiveViewMap(item.View, item.DynamicView, item.ViewModel, item.Data, item.ResultData, bindableViewModel);
	}
}

public record ReactiveViewMap(
		Type? View = null,
		Func<Type?>? DynamicView = null,
		Type? ViewModel = null,
		DataMap? Data = null,
		Type? ResultData = null,
		Type? BindableViewModel = null
	) : ViewMap(View, DynamicView, ViewModel, Data, ResultData)
{
	public override Type? BindingViewModel => BindableViewModel;

	public override void RegisterTypes(IServiceCollection services)
	{
		if (BindableViewModel is not null)
		{
			services.AddTransient(BindableViewModel);
		}

		base.RegisterTypes(services);
	}
}
