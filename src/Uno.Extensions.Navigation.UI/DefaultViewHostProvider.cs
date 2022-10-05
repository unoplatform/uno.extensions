namespace Uno.Extensions.Navigation.UI;

internal class DefaultViewHostProvider : IViewHostProvider
{
	public FrameworkElement CreateViewHost(ContentControl? navigationRoot) => navigationRoot??new ContentControl
	{
		HorizontalAlignment = HorizontalAlignment.Stretch,
		VerticalAlignment = VerticalAlignment.Stretch,
		HorizontalContentAlignment = HorizontalAlignment.Stretch,
		VerticalContentAlignment = VerticalAlignment.Stretch
	};

	public void InitializeViewHost(FrameworkElement contentControl, Task InitialNavigation) { }
}
