//-:cnd:noEmit

namespace MyExtensionsApp.Views;

public sealed partial class MainPage : Page
{
	public MainViewModel? ViewModel { get; private set; }

	public MainPage()
	{
		this.InitializeComponent();

		DataContextChanged += (_, changeArgs) => ViewModel = changeArgs.NewValue as MainViewModel;

	}

	public void GoToSecondPageClick(object sender, RoutedEventArgs arg)
	{
		_ = this.Navigator()?.NavigateViewAsync<SecondPage>(this);
	}
}
