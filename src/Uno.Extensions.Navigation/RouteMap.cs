using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record RouteMap(
	string Path,
	Type? View = null,
	Type? ViewModel = null,
	Type? Data = null,
	Type? ResultData = null,
	bool IsDefault = false,
	Func<NavigationRequest, NavigationRequest>? Init = null,
	Func<object, IDictionary<string, string>>? UntypedToQuery = null,
	Func<IServiceProvider, IDictionary<string, string>, Task<object?>>? UntypedFromQuery = null,
	params RouteMap[] Nested)
{
	internal virtual void RegisterTypes(IServiceCollection services)
	{
		if(ViewModel is not null)
		{
			services.AddTransient(ViewModel);
		}
	}
}


public record RouteMap<TData>(
	string Path,
	Type? View = null,
	Type? ViewModel = null,
	Type? ResultData = null,
	bool IsDefault = false,
	Func<NavigationRequest, NavigationRequest>? Init = null,
	Func<TData, IDictionary<string, string>>? ToQuery = null,
	Func<IServiceProvider, IDictionary<string, string>, Task<TData?>>? FromQuery = null,
	params RouteMap[] Nested) : RouteMap(
		Path,
		View,
		ViewModel,
		typeof(TData),
		ResultData,
		IsDefault,
		Init,
		(object data) => (ToQuery is not null && data is TData tdata) ? ToQuery(tdata) : new Dictionary<string, string>(),
		async (IServiceProvider sp, IDictionary<string, string> query) => await ((FromQuery is not null && query is not null) ? FromQuery(sp, query) : Task.FromResult<TData?>(default)),
		Nested) where TData : class
{
	internal override void RegisterTypes(IServiceCollection services)
	{
		base.RegisterTypes(services);

		services.AddViewModelData<TData>();
	}
}

public record RouteMap<TData, TResultData>(
	string Path,
	Type? View = null,
	Type? ViewModel = null,
	bool IsDefault = false,
	Func<NavigationRequest, NavigationRequest>? Init = null,
	Func<TData, IDictionary<string, string>>? ToQuery = null,
	Func<IServiceProvider, IDictionary<string, string>, Task<TData?>>? FromQuery = null,
	params RouteMap[] Nested) : RouteMap<TData>(
		Path,
		View,
		ViewModel,
		typeof(TResultData),
		IsDefault,
		Init,
		ToQuery,
		FromQuery,
		Nested) where TData : class
{ }

#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

