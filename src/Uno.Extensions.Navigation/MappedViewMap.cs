namespace Uno.Extensions.Navigation;

public record MappedViewMap(
		Type? View = null,
		Func<Type?>? ViewSelector = null,
		Type? ViewModel = null,
		DataMap? Data = null,
		Type? ResultData = null,
		Type? MappedViewModel = null
	) : ViewMap(View, ViewSelector, ViewModel, Data, ResultData)
{
	public override void RegisterTypes(IServiceCollection services)
	{
		if (MappedViewModel is not null)
		{
			services.AddTransient(MappedViewModel);
		}

		base.RegisterTypes(services);
	}
}
