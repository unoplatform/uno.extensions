﻿using System.Xml.Linq;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

public record DataMap(
	Type? Data = null,
	Func<object, IDictionary<string, string>>? UntypedToQuery = null,
	Func<IServiceProvider, IDictionary<string, object>, Task<object?>>? UntypedFromQuery = null
)
{
	public virtual void RegisterTypes(IServiceCollection services)
	{
		if (Data is not null)
		{
			services.AddViewModelData(Data);
		}
	}
}

public record DataMap<TData>(
	Func<TData, IDictionary<string, string>>? ToQuery = null,
	Func<IServiceProvider, IDictionary<string, object>, Task<TData?>>? FromQuery = null
) : DataMap(
	typeof(TData),
	(object data) => (ToQuery is not null && data is TData tdata) ? ToQuery(tdata) : new Dictionary<string, string>(),
	async (IServiceProvider sp, IDictionary<string, object> query) => await ((FromQuery is not null && query is not null) ? FromQuery(sp, query) : Task.FromResult<TData?>(default)))
	where TData : class
{
	public override void RegisterTypes(IServiceCollection services)
	{
		services.AddViewModelData<TData>();
		// DO NOT call base RegisterType method as this will register an untyped version of the data lookup
	}
}



#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

