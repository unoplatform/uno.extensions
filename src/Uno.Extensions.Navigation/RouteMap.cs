using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record RouteMap(
	string Path,
	Type? Data = null,
	Type? ResultData = null,
	Func<NavigationRequest, NavigationRequest>? ProcessRequest = null,
	Func<object, IDictionary<string, string>>? UntypedBuildQuery = null,
	Func<IDictionary<string, string>, Task<object?>>? UntypedLoadData = null)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
	public static RouteMap For(string path) => new RouteMap(path);

}

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record RouteMap<TData>(
	string Path,
	Type? ResultData = null,
	Func<NavigationRequest, NavigationRequest>? ProcessRequest = null,
	Func<TData, IDictionary<string, string>>? BuildQuery = null,
	Func<IDictionary<string, string>, Task<TData?>>? LoadData = null)
	: RouteMap(
		Path,
		typeof(TData),
		ResultData,
		ProcessRequest,
		(object data) => (BuildQuery is not null && data is TData tdata) ? BuildQuery(tdata) : new Dictionary<string, string>(),
		async (IDictionary<string, string> query) => await ((LoadData is not null && query is not null) ? LoadData(query) : Task.FromResult<TData?>(default)))
			where TData : class
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
	public new static RouteMap<TData> For(string path) => new RouteMap<TData>(path);
}

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record RouteMap<TData, TResultData>(
	string Path,
	Func<NavigationRequest, NavigationRequest>? ProcessRequest = null,
	Func<TData, IDictionary<string, string>>? BuildQuery = null,
	Func<IDictionary<string, string>, Task<TData?>>? LoadData = null)
	: RouteMap<TData>(
		Path,
		typeof(TResultData),
		ProcessRequest,
		BuildQuery,
		LoadData)
			where TData : class
			where TResultData : class
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
	public static new RouteMap<TData, TResultData> For(string path) => new RouteMap<TData, TResultData>(path);
}

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record ViewMap(
	string Path,
	Type? ViewType = null,
	Type? ViewModelType = null)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
	public static ViewMap For(string path) => new ViewMap(path);
}


public static class RouteAndViewMapExtensions
{
	public static ViewMap For(this ViewMap map, string path)
	{
		return map with { Path  = path };
	}

	public static ViewMap Show(this ViewMap map, Type viewType)
	{
		return map with { ViewType = viewType };
	}

	public static ViewMap Show<TView>(this ViewMap map)
	{
		return map.Show(typeof(TView));
	}

	public static ViewMap With(this ViewMap map, Type viewModelType)
	{
		return map with { ViewModelType = viewModelType };
	}

	public static ViewMap With<TViewModel>(this ViewMap map)
	{
		return map.With(typeof(TViewModel));
	}

	public static RouteMap For(this RouteMap map, string path)
	{
		return map with { Path = path };
	}


	public static RouteMap Process(this RouteMap map, Func<NavigationRequest, NavigationRequest> processRequest)
	{
		return map with { ProcessRequest = processRequest };
	}

	public static RouteMap<TData> Process<TData>(this RouteMap<TData> map, Func<NavigationRequest, NavigationRequest> processRequest)
		where TData : class
	{
		return map with { ProcessRequest = processRequest };
	}


	public static RouteMap<TData> ConvertDataToQuery<TData>(this RouteMap<TData> map, Func<TData, IDictionary<string, string>>? processRequest)
		where TData : class
	{
		return map with { BuildQuery = processRequest };
	}

	public static RouteMap<TData> ConvertQueryToData<TData>(this RouteMap<TData> map, Func<IDictionary<string, string>, Task<TData?>>? processRequest)
		where TData : class
	{
		return map with { LoadData = processRequest };
	}
}
