namespace Uno.Extensions.Navigation.UI;

internal class DefaultViewHostProvider : IViewHostProvider, IDeferrable
{
	public FrameworkElement CreateViewHost() => new ContentControl
	{
		HorizontalAlignment = HorizontalAlignment.Stretch,
		VerticalAlignment = VerticalAlignment.Stretch,
		HorizontalContentAlignment = HorizontalAlignment.Stretch,
		VerticalContentAlignment = VerticalAlignment.Stretch
	};
	public Deferral GetDeferral() => new Deferral(() => { });

	public IDeferrable InitializeViewHost(FrameworkElement contentControl, Task InitialNavigation) { return this; }
}
