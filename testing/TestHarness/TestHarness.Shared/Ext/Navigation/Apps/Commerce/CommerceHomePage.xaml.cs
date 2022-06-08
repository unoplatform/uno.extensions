namespace TestHarness.Ext.Navigation.Apps.Commerce;

public sealed partial class CommerceHomePage : Page
{
	public CommerceHomePage()
	{
		this.InitializeComponent();

		this.ApplyAdaptiveTrigger(App.Current.Resources["WideMinWindowWidth"] is double width ? width : 0.0, nameof(Narrow), nameof(Wide));
	}
}
