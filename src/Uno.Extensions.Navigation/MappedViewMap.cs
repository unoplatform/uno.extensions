using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions.Navigation;

internal record MappedViewMap(
		Type? View = null,
		Func<Type?>? ViewSelector = null,
		[param:   DynamicallyAccessedMembers(Uno.Extensions.Diagnostics.Annotations.ViewModelRequirements)]
		Type? ViewModel = null,
		DataMap? Data = null,
		Type? ResultData = null,
		[param:   DynamicallyAccessedMembers(Uno.Extensions.Diagnostics.Annotations.ViewModelRequirements)]
		[property:DynamicallyAccessedMembers(Uno.Extensions.Diagnostics.Annotations.ViewModelRequirements)]
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
