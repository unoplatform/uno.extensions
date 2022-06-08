namespace TestHarness.Ext.Navigation.Apps.Commerce;

public sealed partial class CommerceProductsPage : Page
{
	public CommerceProductsViewModel? ViewModel => DataContext as CommerceProductsViewModel;

	public CommerceProductsPage()
	{
		this.InitializeComponent();

		this.ApplyAdaptiveTrigger(App.Current.Resources["WideMinWindowWidth"] is double width ? width : 0.0, nameof(Narrow), nameof(Wide));
	}
}
