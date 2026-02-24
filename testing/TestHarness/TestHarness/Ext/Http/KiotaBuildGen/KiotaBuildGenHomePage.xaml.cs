namespace TestHarness.Ext.Http.KiotaBuildGen;

public sealed partial class KiotaBuildGenHomePage : Page
{
	internal KiotaBuildGenHomeViewModel? ViewModel => DataContext as KiotaBuildGenHomeViewModel;

	public KiotaBuildGenHomePage()
	{
		this.InitializeComponent();
	}
}
