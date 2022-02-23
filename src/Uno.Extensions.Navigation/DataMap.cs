namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

public record DataMap(
	Type? Data = null,
		Func<object, IDictionary<string, string>>? UntypedToQuery = null,
	Func<IServiceProvider, IDictionary<string, string>, Task<object?>>? UntypedFromQuery = null
)
{
	internal virtual void RegisterTypes(IServiceCollection services)
	{
	}
}

public record DataMap<TData>(
	Func<TData, IDictionary<string, string>>? ToQuery = null,
	Func<IServiceProvider, IDictionary<string, string>, Task<TData?>>? FromQuery = null
) : DataMap(
	typeof(TData),
	(object data) => (ToQuery is not null && data is TData tdata) ? ToQuery(tdata) : new Dictionary<string, string>(),
	async (IServiceProvider sp, IDictionary<string, string> query) => await ((FromQuery is not null && query is not null) ? FromQuery(sp, query) : Task.FromResult<TData?>(default)))
	where TData : class
{
	internal override void RegisterTypes(IServiceCollection services)
	{
		services.AddViewModelData<TData>();
	}
}



#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

