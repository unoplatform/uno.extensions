using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record RouteMap(
	string Path,
	Type? ViewType = null,
	Func<NavigationRequest, NavigationRequest>? Init = null)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
}


#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record ViewMap(
	Type ViewType,
	Type? ViewModelType = null,
	Type? Data = null,
	Type? ResultData = null,
	Func<object, IDictionary<string, string>>? UntypedBuildQuery = null,
	Func<IDictionary<string, string>, Task<object?>>? UntypedLoadData = null)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
}

public record ViewMap<TData>(
	Type ViewType,
	Type? ViewModelType = null,
	Type? ResultData = null,
	Func<TData, IDictionary<string, string>>? BuildQuery = null,
	Func<IDictionary<string, string>, Task<TData?>>? LoadData = null) : ViewMap(
		ViewType,
		ViewModelType,
		typeof(TData),
		ResultData,
		(object data) => (BuildQuery is not null && data is TData tdata) ? BuildQuery(tdata) : new Dictionary<string, string>(),
		async (IDictionary<string, string> query) => await ((LoadData is not null && query is not null) ? LoadData(query) : Task.FromResult<TData?>(default)))
			where TData : class
{ }

public record ViewMap<TData, TResultData>(
	Type ViewType,
	Type? ViewModelType = null,
	Func<TData, IDictionary<string, string>>? BuildQuery = null,
	Func<IDictionary<string, string>, Task<TData?>>? LoadData = null) : ViewMap<TData>(
		ViewType,
		ViewModelType,
		typeof(TResultData),
		BuildQuery,
		LoadData)
			where TData : class
{ }
