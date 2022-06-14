
namespace TestHarness.Ext.Navigation.Responsive;

public sealed partial class ResponsiveListPage : Page
{
	public ResponsiveListPage()
	{
		this.InitializeComponent();

		this.ApplyAdaptiveTrigger(App.Current.Resources["WideMinWindowWidth"] is double width ? width : 0.0, nameof(Narrow), nameof(Wide));
	}
}
