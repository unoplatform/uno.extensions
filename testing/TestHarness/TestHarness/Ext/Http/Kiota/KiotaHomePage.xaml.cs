namespace TestHarness.Ext.Http.Kiota
{

	public sealed partial class KiotaHomePage : Page
	{
		internal KiotaHomeViewModel? ViewModel => DataContext as KiotaHomeViewModel;

		public KiotaHomePage()
		{
			this.InitializeComponent();
		}
	}
}
