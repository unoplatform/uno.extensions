namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record ViewMap(
	Func<Type?>? DynamicView = null,
	Type? ViewModel = null,
	DataMap? Data = null,
	Type? ResultData = null
)
{
	public Type? View => DynamicView?.Invoke();

	internal void RegisterTypes(IServiceCollection services)
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
	Type? ResultData = null
) : ViewMap(() => typeof(TView), ViewModel, Data, ResultData)
{
}

public record ViewMap<TView, TViewModel>(
	DataMap? Data = null,
	Type? ResultData = null
) : ViewMap(() => typeof(TView), typeof(TViewModel), Data, ResultData)
{
}

#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

