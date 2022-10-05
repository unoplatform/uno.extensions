namespace Uno.Extensions.Navigation.UI;

internal interface IViewHostProvider
{
	FrameworkElement CreateViewHost(ContentControl? navigationRoot);

	void InitializeViewHost(FrameworkElement contentControl, Task InitialNavigation);
}
