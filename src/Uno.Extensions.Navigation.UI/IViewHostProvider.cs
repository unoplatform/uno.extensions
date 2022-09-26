namespace Uno.Extensions.Navigation.UI;

internal interface IViewHostProvider
{
	FrameworkElement CreateViewHost();

	IDeferrable InitializeViewHost(FrameworkElement contentControl, Task InitialNavigation);
}
