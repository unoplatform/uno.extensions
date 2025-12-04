using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record ViewMap(
	Type? View = null,
	Func<Type?>? ViewSelector = null,
	[param:   DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicConstructors)]
	[property:DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicConstructors)]
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

		RegisterResultDataType(services);
	}

	internal virtual void RegisterResultDataType(IServiceCollection services)
	{
		if (ResultData is not null)
		{
			services.AddViewModelData(ResultData);
		}

	}
}

public record ViewMap<TView>(
	[param:   DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicConstructors)]
	Type? ViewModel = null,
	DataMap? Data = null,
	Type? ResultData = null,
	object? ViewAttributes = null
) : ViewMap(
	View: typeof(TView),
	ViewSelector: null,
	ViewModel: ViewModel,
	Data: Data,
	ResultData: ResultData,
	ViewAttributes: ViewAttributes
)
	where TView: class, new()
{
	public override void RegisterTypes(IServiceCollection services)
	{
		services.AddTransient<TView>(sp => new TView());
		base.RegisterTypes(services);
	}
}

public record ViewMap<
	TView,
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	TViewModel
>(
	DataMap? Data = null,
	Type? ResultData = null,
	object? ViewAttributes = null
) : ViewMap<TView>(ViewModel: typeof(TViewModel), Data: Data, ResultData: ResultData, ViewAttributes: ViewAttributes)
	where TView : class, new()
{
}

public record DataViewMap<
	TView,
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	TViewModel,
	TData
>(
	Func<TData, IDictionary<string, string>>? ToQuery = null,
	Func<IServiceProvider, IDictionary<string, object>, Task<TData?>>? FromQuery = null,
	Type? ResultData = null,
	object? ViewAttributes = null
) : ViewMap<TView,TViewModel>(Data: new DataMap<TData>(ToQuery, FromQuery), ResultData: ResultData, ViewAttributes: ViewAttributes)
	where TView : class, new()
	where TData : class
{
}

public record ResultDataViewMap<
	TView,
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	TViewModel,
	TResultData
>(
	DataMap? Data = null,
	object? ViewAttributes = null
) : ViewMap<TView,TViewModel>(Data: Data, ResultData: typeof(TResultData), ViewAttributes: ViewAttributes)
	where TView : class, new()
	where TResultData : class
{
	internal override void RegisterResultDataType(IServiceCollection services)
	{
		services.AddViewModelData<TResultData>();
		// DO NOT call base RegisterType method as this will register an untyped version of the data lookup
	}
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


