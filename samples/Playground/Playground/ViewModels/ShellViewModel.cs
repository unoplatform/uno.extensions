namespace Playground.ViewModels;

public class ShellViewModel
{
	public ShellViewModel(INavigator navigator)
	{
		// To switch options, change the Route info in App.xaml.host.cs
		// Option 1: Specify ShellView in order to customise the shell. Navigation needs to be nested so
		// that it loads the page in the nested Frame in the ShellView
		//navigator.NavigateViewAsync<HomePage>(this, qualifier: Qualifiers.Nested);

		// Option 2: Only specify the ShellViewModel - this will inject a FrameView and so
		// the INavigator corresponds to the Frame (ie no need to nest navigation)
		//navigator.NavigateViewAsync<HomePage>(this);
	}
}
