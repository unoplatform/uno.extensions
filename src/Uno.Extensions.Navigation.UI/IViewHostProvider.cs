namespace Uno.Extensions.Navigation.UI;

internal interface IViewHostProvider
{
	void InitializeViewHost(FrameworkElement contentControl, Task InitialNavigation);
}
