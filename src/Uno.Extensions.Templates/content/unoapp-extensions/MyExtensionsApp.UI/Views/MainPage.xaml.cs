//-:cnd:noEmit

namespace MyExtensionsApp.Views;

public sealed partial class MainPage : Page, IInjectable<INavigator>
{
	public MainViewModel? ViewModel { get; private set; }

	public MainPage()
	{
		this.InitializeComponent();

		DataContextChanged += (_, changeArgs) => ViewModel = changeArgs.NewValue as MainViewModel;

	}

	public void GoToSecondPageClick(object sender, RoutedEventArgs arg)
	{
		_navigator.NavigateViewAsync<SecondPage>(this);
	}

	public void Inject(INavigator navigator)
	{
		_navigator = navigator;
	}

	private INavigator? _navigator;
}
