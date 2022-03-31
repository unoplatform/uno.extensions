namespace Uno.Extensions.Navigation;

public class ViewRegistry : Registry<ViewMap>, IViewRegistry
{
	public ViewRegistry(IServiceCollection services) : base(services)
	{
	}

	protected override void InsertItem(ViewMap item)
	{
		base.InsertItem(item);
		item.RegisterTypes(Services);
	}
}
