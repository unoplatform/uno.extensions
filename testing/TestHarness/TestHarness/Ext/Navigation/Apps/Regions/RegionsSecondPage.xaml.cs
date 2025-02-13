namespace TestHarness.Ext.Navigation.Apps.Regions;

public sealed partial class RegionsSecondPage : Page
{
	public RegionsSecondViewModel ViewModel => (RegionsSecondViewModel)DataContext;
	public RegionsSecondPage()
	{
		this.InitializeComponent();
	}
}
