using System.Xml.Linq;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

/// <summary>
/// Registers a data type for injection during navigation and optionally maps the
/// data to/from a query string.
/// </summary>
/// <param name="Data">
/// The data type to register. If null, the data type will not be registered.
/// </param>
/// <param name="UntypedToQuery">
/// A function that maps the data to a query string. If null, the data will not be
/// mapped to a query string.
/// </param>
/// <param name="UntypedFromQuery">
/// A function that maps a query string to the data. If null, no relationship will be 
/// established between the query string and data.
/// </param>
public record DataMap(
	Type? Data = null,
	Func<object, IDictionary<string, string>>? UntypedToQuery = null,
	Func<IServiceProvider, IDictionary<string, object>, Task<object?>>? UntypedFromQuery = null
)
{
	internal virtual void RegisterTypes(IServiceCollection services)
	{
		if (Data is not null)
		{
			services.AddViewModelData(Data);
		}
	}
}

/// <summary>
/// A strongly-typed version of <see cref="DataMap"/> that registers a data type 
/// for injection during navigation and optionally maps the data to/from a query string.
/// </summary>
/// <typeparam name="TData">
/// The data type to register.
/// </typeparam>
/// <param name="ToQuery">
/// A function that maps the data of type <typeparamref name="TData"/> to a query string.
/// If null, the data will not be mapped to a query string.
/// </param>
/// <param name="FromQuery">
/// A function that maps a query string to the data of type <typeparamref name="TData"/>.
/// If null, no relationship will be established between the query string and data.
/// </param>
public record DataMap<TData>(
	Func<TData, IDictionary<string, string>>? ToQuery = null,
	Func<IServiceProvider, IDictionary<string, object>, Task<TData?>>? FromQuery = null
) : DataMap(
	typeof(TData),
	(object data) => (ToQuery is not null && data is TData tdata) ? ToQuery(tdata) : new Dictionary<string, string>(),
	async (IServiceProvider sp, IDictionary<string, object> query) => await ((FromQuery is not null && query is not null) ? FromQuery(sp, query) : Task.FromResult<TData?>(default)))
	where TData : class
{
	internal override void RegisterTypes(IServiceCollection services)
	{
		services.AddViewModelData<TData>();
		// DO NOT call base RegisterType method as this will register an untyped version of the data lookup
	}
}



#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

