namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record ViewMap(
		Type? View = null,
	Type? ViewModel = null,
	DataMap? Data = null,
	Type? ResultData = null
)
{
	internal void RegisterTypes(IServiceCollection services)
	{
		if (ViewModel is not null)
		{
			services.AddTransient(ViewModel);
		}

		Data?.RegisterTypes(services);
	}
}



#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

