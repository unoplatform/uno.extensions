using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions.Navigation;

internal record MappedViewMap(
		Type? View = null,
		Func<Type?>? ViewSelector = null,
		Type? ViewModel = null,
		DataMap? Data = null,
		Type? ResultData = null,
		[param:   DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicConstructors)]
		[property:DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicConstructors)]
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
