namespace Uno.Extensions.Navigation;

public class MappedViewRegistry : ViewRegistry
{
	public IDictionary<Type, Type> ViewModelMappings { get; }
	public MappedViewRegistry(IServiceCollection services, IDictionary<Type, Type> viewModelMappings) : base(services)
	{
		ViewModelMappings = viewModelMappings;
	}

	protected override void InsertItem(ViewMap item)
	{
		if (item.ViewModel is not null &&
			ViewModelMappings.TryGetValue(item.ViewModel, out var bindableViewModel))
		{
			item = new MappedViewMap(item.View, item.ViewSelector, item.ViewModel, item.Data, item.ResultData, bindableViewModel);
		}

		base.InsertItem(item);
	}
}
