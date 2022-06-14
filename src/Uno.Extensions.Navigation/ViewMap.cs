namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record ViewMap(
	Type? View = null,
	Func<Type?>? ViewSelector = null,
	Type? ViewModel = null,
	DataMap? Data = null,
	Type? ResultData = null,
	object? ViewAttributes = null
)
{
	public virtual void RegisterTypes(IServiceCollection services)
	{
		if (ViewModel is not null)
		{
			services.AddTransient(ViewModel);
		}

		Data?.RegisterTypes(services);
	}
}

public record ViewMap<TView>(
	Type? ViewModel = null,
	DataMap? Data = null,
	Type? ResultData = null,
	object? ViewAttributes = null
) : ViewMap(View: typeof(TView), ViewModel: ViewModel, Data: Data, ResultData: ResultData, ViewAttributes: ViewAttributes)
{
}

public record ViewMap<TView, TViewModel>(
	DataMap? Data = null,
	Type? ResultData = null,
	object? ViewAttributes = null
) : ViewMap(View: typeof(TView), ViewModel: typeof(TViewModel), Data: Data, ResultData: ResultData, ViewAttributes: ViewAttributes)
{
}

public record DataViewMap<TView, TViewModel, TData>(
	Func<TData, IDictionary<string, string>>? ToQuery = null,
	Func<IServiceProvider, IDictionary<string, object>, Task<TData?>>? FromQuery = null,
	Type? ResultData = null,
	object? ViewAttributes = null
) : ViewMap(View: typeof(TView), ViewModel: typeof(TViewModel), Data: new DataMap<TData>(ToQuery, FromQuery), ResultData: ResultData, ViewAttributes: ViewAttributes)
	where TData : class
{
}

public record ResultDataViewMap<TView, TViewModel, TResultData>(
	DataMap? Data = null,
	object? ViewAttributes = null
) : ViewMap(View: typeof(TView), ViewModel: typeof(TViewModel), Data: Data, ResultData: typeof(TResultData), ViewAttributes: ViewAttributes)
{
}

public record LocalizableDialogAction(Func<IStringLocalizer?, string?>? LabelProvider = default, Action? Action = null, object? Id = null) { }

public record DialogAction(string? Label = default, Action? Action = null, object? Id = null)
	: LocalizableDialogAction(
		LabelProvider: _ => Label,
		Action,
		Id
		)
{ }

#pragma warning restore SA1313 // Parameter names should begin with lower-case letter


