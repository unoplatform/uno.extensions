
namespace TestHarness.Ext.Navigation.Apps.Commerce;

public sealed partial class CommerceDealsPage : Page
{
	public CommerceDealsViewModel? ViewModel => DataContext as CommerceDealsViewModel;
	public CommerceDealsPage()
	{
		this.InitializeComponent();

		this.ApplyAdaptiveTrigger(App.Current.Resources["WideMinWindowWidth"] is double width ? width : 0.0, nameof(Narrow), nameof(Wide));
	}
}
