
namespace TestHarness.Ext.Navigation.Reactive;

public sealed partial class ReactiveOnePage : Page
{
	public ReactiveOneViewModel.BindableReactiveOneViewModel? ViewModel => DataContext as ReactiveOneViewModel.BindableReactiveOneViewModel;
	public ReactiveOnePage()
	{
		this.InitializeComponent();
	}

	public async void OnePageToTwoPageCodebehindClick(object sender, RoutedEventArgs e)
	{
		await this.Navigator()!.NavigateViewAsync<ReactiveTwoPage>(this);
	}

}
